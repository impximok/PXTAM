/*
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using SnomiAssignmentReal.Data;
using SnomiAssignmentReal.Helpers;
using SnomiAssignmentReal.Models;
using SnomiAssignmentReal.Models.ViewModels;
using System.Security.Claims;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.IO;

namespace SnomiAssignmentReal.Controllers
{
    [Route("Customer")]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _cfg;
        private readonly HCaptchaVerifier _hCaptcha;
        private readonly IMemoryCache _cache;

        // Lockout settings
        private const int MAX_BAD_ATTEMPTS = 3;                       // lock after 3 bad tries
        private static readonly TimeSpan LOCKOUT_TIME = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan ATTEMPT_TTL = TimeSpan.FromMinutes(10);

        private string AttemptKey(string loginKey, string? ip) => $"login:attempts:{loginKey}:{ip}";
        private string LockKey(string loginKey, string? ip) => $"login:lock:{loginKey}:{ip}";

        public CustomerController(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            IConfiguration cfg,
            HCaptchaVerifier hCaptcha,
            IMemoryCache cache)
        {
            _context = context;
            _env = env;
            _cfg = cfg;
            _hCaptcha = hCaptcha;
            _cache = cache;
        }

        // ----------------------- ENTRY -----------------------
        [HttpGet("Entry")]
        public IActionResult CustomerEntry() => View();

        // ----------------------- GUEST FLOW -----------------------
        [AllowAnonymous]
        [HttpGet("ContinueAsGuest")]
        public async Task<IActionResult> ContinueAsGuest(string returnUrl = "/Menu/Catalog")
        {
            await HttpContext.SignOutAsync("MyCookieAuth");
            HttpContext.Session.Clear();

            var guestId = "G" + Guid.NewGuid().ToString("N")[..9];
            var guest = new Customer { CustomerId = guestId, CustomerFullName = "Guest", IsCustomerLoggedIn = false };
            _context.Customers.Add(guest);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("IsGuest", "true");
            HttpContext.Session.SetString("GuestCustomerId", guestId);
            HttpContext.Session.SetString("CustomerId", guestId);

            return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? "/Menu/Catalog" : returnUrl);
        }

        // ----------------------- REGISTER (with hCaptcha) -----------------------
        [HttpGet("Register")]
        [AllowAnonymous]
        public IActionResult Register()
        {
            ViewBag.HCaptchaSiteKey = _cfg["Captcha:hCaptcha:SiteKey"];
            return View(new RegisterViewModel());
        }

        [HttpPost("Register")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            ViewBag.HCaptchaSiteKey = _cfg["Captcha:hCaptcha:SiteKey"];
            if (!ModelState.IsValid) return View(model);

            // hCaptcha check
            var token = Request.Form["h-captcha-response"].ToString();
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var human = await _hCaptcha.VerifyAsync(token, remoteIp);
            if (!human)
            {
                ModelState.AddModelError(string.Empty, "Captcha verification failed. Please try again.");
                return View(model);
            }

            var normalizedEmail = model.Email?.Trim().ToLowerInvariant();

            // Uniqueness
            if (await _context.Customers.AnyAsync(c => c.CustomerUserName == model.UserName))
            {
                ModelState.AddModelError(nameof(model.UserName), "Username already taken.");
                return View(model);
            }
            if (!string.IsNullOrWhiteSpace(normalizedEmail) &&
                await _context.Customers.AnyAsync(c => c.CustomerEmailAddress != null && c.CustomerEmailAddress.ToLower() == normalizedEmail))
            {
                ModelState.AddModelError(nameof(model.Email), "CustomerEmailAddress already in use.");
                return View(model);
            }

            // ========================= IMAGE HANDLING (Upload OR Camera) =========================
            // start with a random guest avatar
            string relativeImagePath = GetRandomGuestAvatarOrDefault();


            // 1) Prefer normal file upload
            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                var error = FileHelper.ValidateImage(model.ProfileImage);
                if (!string.IsNullOrEmpty(error))
                {
                    ModelState.AddModelError(nameof(model.ProfileImage), error);
                    return View(model);
                }
                var fileName = FileHelper.SaveFile(model.ProfileImage, "images/users", _env.WebRootPath);
                relativeImagePath = $"/images/users/{fileName}";
            }
            // 2) Else use camera snapshot (Base64 data URL)
            else if (!string.IsNullOrWhiteSpace(model.CapturedImageData))
            {
                var (bytes, ext, err) = FileHelper.ParseDataUrl(model.CapturedImageData, maxBytes: 2 * 1024 * 1024);
                if (!string.IsNullOrEmpty(err) || bytes == null)
                {
                    ModelState.AddModelError(nameof(model.ProfileImage), err ?? "Invalid camera image.");
                    return View(model);
                }
                var fileName = FileHelper.SaveBytes(bytes, "images/users", _env.WebRootPath, ext);
                relativeImagePath = $"/images/users/{fileName}";
            }
            // =====================================================================================

            var newCustomer = new Customer
            {
                CustomerId = GenerateCustomerId(),
                CustomerFullName = model.Name?.Trim(),
                CustomerUserName = model.UserName,
                CustomerEmailAddress = normalizedEmail,
                CustomerPhoneNumber = model.PhoneNumber,
                CustomerPasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                CustomerProfileImageUrl = relativeImagePath,
                IsCustomerLoggedIn = false
            };

            _context.Customers.Add(newCustomer);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Registration successful. Please login.";
            return RedirectToAction("Login", "Customer");
        }


        private string GenerateCustomerId()
        {
            var next = (_context.Customers.Count() + 1);
            return $"C{next:D4}";
        }

        // ----------------------- LOGIN (hCaptcha + lockout) -----------------------
        [HttpGet("Login")]
        [AllowAnonymous]
        public IActionResult Login()
        {
            ViewBag.HCaptchaSiteKey = _cfg["Captcha:hCaptcha:SiteKey"];
            return View(new LoginVm());
        }

        [HttpPost("Login")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVm vm, string? guest)
        {
            if (!string.IsNullOrEmpty(guest))
                return RedirectToAction(nameof(ContinueAsGuest), new { returnUrl = "/Menu" });

            ViewBag.HCaptchaSiteKey = _cfg["Captcha:hCaptcha:SiteKey"];
            if (!ModelState.IsValid) return View(vm);

            // Normalize identity key + capture IP
            var rawKey = (vm.Email ?? string.Empty).Trim();
            var loginKey = rawKey.ToLowerInvariant();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // 1) Lockout check
            if (_cache.TryGetValue<DateTimeOffset>(LockKey(loginKey, ip), out var untilUtc) &&
                untilUtc > DateTimeOffset.UtcNow)
            {
                var seconds = (int)(untilUtc - DateTimeOffset.UtcNow).TotalSeconds;
                ModelState.AddModelError(string.Empty, $"Too many failed attempts. Try again in {seconds} seconds.");
                return View(vm);
            }

            // 2) hCaptcha
            var token = Request.Form["h-captcha-response"].ToString();
            var human = await _hCaptcha.VerifyAsync(token, ip);
            if (!human)
            {
                ModelState.AddModelError(string.Empty, "Captcha verification failed. Please try again.");
                return View(vm);
            }

            // 3) Lookup & verify
            var cust = await _context.Customers.FirstOrDefaultAsync(c =>
                (c.CustomerEmailAddress != null && c.CustomerEmailAddress.ToLower() == loginKey) ||
                (c.CustomerUserName != null && c.CustomerUserName.ToLower() == loginKey)
            );

            var passwordOk = cust != null &&
                             !string.IsNullOrEmpty(cust.CustomerPasswordHash) &&
                             BCrypt.Net.BCrypt.Verify(vm.Password, cust.CustomerPasswordHash);

            if (passwordOk)
            {
                // Success: clear counters
                _cache.Remove(AttemptKey(loginKey, ip));
                _cache.Remove(LockKey(loginKey, ip));

                HttpContext.Session.Remove("IsGuest");
                HttpContext.Session.Remove("GuestCustomerId");
                HttpContext.Session.SetString("CustomerId", cust!.CustomerId);
                HttpContext.Session.SetString("CustomerUserName", cust.CustomerUserName ?? cust.CustomerFullName ?? "Customer");

                var photoUrl = NormalizePhoto(cust.CustomerProfileImageUrl, bust: true);

                HttpContext.Session.SetString("CustomerPhotoUrl", photoUrl);

                var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, cust.CustomerId),
    new Claim(ClaimTypes.Name, cust.CustomerUserName ?? cust.CustomerFullName ?? "Customer"),
    new Claim(ClaimTypes.Role, "Customer"),
    new Claim("photo", photoUrl)
};


                var identity = new ClaimsIdentity(claims, "MyCookieAuth");
                await HttpContext.SignInAsync("MyCookieAuth", new ClaimsPrincipal(identity));
                return RedirectToAction("Catalog", "Menu");
            }

            // Failed: increase attempt count & maybe lock
            var attemptsKey = AttemptKey(loginKey, ip);
            var attempts = _cache.Get<int?>(attemptsKey) ?? 0;
            attempts++;

            _cache.Set(attemptsKey, attempts, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ATTEMPT_TTL
            });

            if (attempts >= MAX_BAD_ATTEMPTS)
            {
                var lockUntil = DateTimeOffset.UtcNow.Add(LOCKOUT_TIME);
                _cache.Set(LockKey(loginKey, ip), lockUntil, lockUntil);
                ModelState.AddModelError(string.Empty,
                    $"Too many failed attempts. Your login is locked for {(int)LOCKOUT_TIME.TotalMinutes} minutes.");
            }
            else
            {
                var left = MAX_BAD_ATTEMPTS - attempts;
                ModelState.AddModelError(nameof(vm.Password), "Incorrect password.");
                ModelState.AddModelError(string.Empty,
                    $"Login failed. {left} attempt{(left == 1 ? "" : "s")} remaining before lockout.");
            }

            return View(vm);
        }

        [HttpPost("Logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync("MyCookieAuth");
            return RedirectToAction("Welcome", "Home");
        }

        // ----------------------- RESET PASSWORD -----------------------
        [HttpGet("ResetPassword")]
        [AllowAnonymous]
        public IActionResult ResetPassword() => View(new ResetPasswordVm());

        [HttpPost("ResetPassword")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var email = (vm.Email ?? "").Trim().ToLowerInvariant();

            var cust = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerEmailAddress != null && c.CustomerEmailAddress.ToLower() == email);

            if (cust == null)
            {
                ModelState.AddModelError(nameof(vm.Email), "No customer found with this email.");
                return View(vm);
            }

            cust.PasswordResetToken = Guid.NewGuid().ToString();
            cust.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(30);

            _context.Customers.Update(cust);
            await _context.SaveChangesAsync();

            SendResetPasswordEmail(cust);
            TempData["Info"] = "A reset link has been sent to your email.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet("SetNewPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> SetNewPassword(string token, string email)
        {
            var nowUtc = DateTime.UtcNow;

            var cust = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.PasswordResetToken == token &&
                    c.PasswordResetTokenExpiry != null &&
                    c.PasswordResetTokenExpiry >= nowUtc
                );

            if (cust == null)
            {
                TempData["Error"] = "Invalid or expired token.";
                return RedirectToAction(nameof(ResetPassword));
            }

            return View(new SetNewPasswordVm { Email = cust.CustomerEmailAddress, Token = token });
        }

        [HttpPost("SetNewPassword")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetNewPassword(SetNewPasswordVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var nowUtc = DateTime.UtcNow;

            var cust = await _context.Customers
                .FirstOrDefaultAsync(c =>
                    c.PasswordResetToken == vm.Token &&
                    c.PasswordResetTokenExpiry != null &&
                    c.PasswordResetTokenExpiry >= nowUtc
                );

            if (cust == null)
            {
                TempData["Error"] = "Invalid or expired token.";
                return RedirectToAction(nameof(ResetPassword));
            }

            cust.CustomerPasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.NewPassword);
            cust.PasswordResetToken = null;
            cust.PasswordResetTokenExpiry = null;

            _context.Customers.Update(cust);
            await _context.SaveChangesAsync();

            TempData["Info"] = "Your password has been updated.";
            return RedirectToAction(nameof(Login));
        }

        private void SendResetPasswordEmail(Customer cust)
        {
            // Configurable sender & support
            var fromAddress = _cfg["CustomerEmailAddress:FromAddress"] ?? "no-reply@snomi.example";
            var fromName = _cfg["CustomerEmailAddress:FromName"] ?? "Snömi Café";
            var supportEmail = _cfg["CustomerEmailAddress:Support"] ?? "support@snomi.example";

            // Build absolute reset link
            string resetLink = Url.Action("SetNewPassword", "Customer",
                new { token = cust.PasswordResetToken, email = cust.CustomerEmailAddress }, Request.Scheme) ?? string.Empty;

            // Encode dynamic values
            string encName = WebUtility.HtmlEncode(cust.CustomerFullName ?? cust.CustomerUserName ?? "Customer");
            string encLink = WebUtility.HtmlEncode(resetLink);
            string encSupp = WebUtility.HtmlEncode(supportEmail);
            string nowUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'");

            string subject = "HashedPassword Reset — Snömi Café";
            string preheader = "Reset your password with the secure link below. Expires in 30 minutes.";

            // Plain-text fallback (deliverability + accessibility)
            string textBody = $@"{subject}

Hello {cust.CustomerFullName ?? cust.CustomerUserName ?? "Customer"},

We received a request to reset your Snömi Café password.

Reset link (valid 30 minutes):
{resetLink}

If you didn’t request this, ignore this message. Your password remains unchanged.

Requested: {nowUtc}
Support: {supportEmail}
";

            // Minimalist black & white HTML (Apple-y vibes)
            var html = new StringBuilder();
            html.Append($@"
<!doctype html>
<html lang=""en"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <title>{WebUtility.HtmlEncode(subject)}</title>
  <style>
    body {{ margin:0; background:#fff; }}
    .wrap {{ padding:32px 16px; background:#fff; }}
    .card {{
      max-width:640px; margin:0 auto; background:#fff; color:#000;
      font-family:-apple-system, BlinkMacSystemFont, 'SF Pro Text', 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
      line-height:1.6; font-size:16px; letter-spacing:.01em;
      border:1px solid #e6e6e6; border-radius:14px; padding:32px;
    }}
    .preheader {{ display:none; visibility:hidden; opacity:0; color:transparent; height:0; max-height:0; overflow:hidden; }}
    .brand {{ font-weight:600; font-size:14px; letter-spacing:.06em; text-transform:uppercase; margin:0 0 8px; }}
    .h1 {{ font-size:28px; font-weight:700; letter-spacing:-.02em; margin:8px 0 16px; }}
    .muted {{ color:#555; }}
    .rule {{ border-top:1px solid #e6e6e6; margin:24px 0; }}
    .btn {{
      display:inline-block; background:#000; color:#fff !important; text-decoration:none;
      padding:14px 22px; border-radius:9999px; font-weight:600; letter-spacing:.01em;
    }}
    .link {{ word-break:break-all; font-family:ui-monospace, SFMono-Regular, Menlo, Consolas, monospace; font-size:13px; color:#111; }}
    .fineprint {{ color:#666; font-size:13px; }}
    .sp16 {{ height:16px; }}
  </style>
</head>
<body>
  <div class=""wrap"">
    <div class=""preheader"">{WebUtility.HtmlEncode(preheader)}</div>
    <div class=""card"">
      <div class=""brand"">SNÖMI CAFÉ</div>
      <div class=""h1"">HashedPassword reset</div>

      <p>Hello <strong>{encName}</strong>,</p>
      <p class=""muted"">We received a request to reset the password for your Snömi Café account.</p>

      <div class=""sp16""></div>
      <p><a class=""btn"" href=""{encLink}"">Reset password</a></p>

      <div class=""sp16""></div>
      <p class=""fineprint""><strong>Expires in 30 minutes.</strong> For security, this link can be used once.</p>

      <div class=""rule""></div>

      <p class=""muted"">If the button doesn’t work, paste this link into your browser:</p>
      <p class=""link"">{encLink}</p>

      <div class=""rule""></div>

      <p class=""fineprint"">
        If you didn’t request this, you can ignore this email and your password will remain unchanged.
        <br>Requested: {WebUtility.HtmlEncode(nowUtc)}
        <br>Need help? <a href=""mailto:{encSupp}"" style=""color:#000;text-decoration:underline;"">{encSupp}</a>
      </p>

      <p class=""fineprint"">— Snömi Café</p>
    </div>
  </div>
</body>
</html>");

            var mail = new MailMessage
            {
                Subject = subject,
                From = new MailAddress(fromAddress, fromName),
                IsBodyHtml = true
            };

            mail.To.Add(new MailAddress(cust.CustomerEmailAddress ?? string.Empty, cust.CustomerFullName ?? cust.CustomerUserName ?? "Customer"));
            mail.ReplyToList.Add(new MailAddress(supportEmail, "Snömi Café Support"));

            var htmlView = AlternateView.CreateAlternateViewFromString(html.ToString(), Encoding.UTF8, "text/html");
            var textView = AlternateView.CreateAlternateViewFromString(textBody, Encoding.UTF8, "text/plain");
            mail.AlternateViews.Add(textView);
            mail.AlternateViews.Add(htmlView);

            // Helpful headers
            mail.Headers.Add("X-Auto-Response-Suppress", "All");
            mail.Headers.Add("List-Unsubscribe", $"<{encSupp}>");

            EmailHelper.SendEmail(mail, _cfg);
        }

        private void AddGeneralErrorIfNone()
        {
            if (!ModelState.ErrorCount.Equals(0) && !ModelState.ContainsKey(string.Empty))
                ModelState.AddModelError(string.Empty, "Please fix the errors below and try again.");
        }


        // ----------------------- UPDATE PROFILE -----------------------
        [Authorize(Roles = "Customer")]
        [HttpGet("Profile")]
        public async Task<IActionResult> Profile()
        {
            var cust = await GetCurrentCustomerAsync();
            if (cust == null) return RedirectToAction(nameof(Login));

            var bust = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var photo = string.IsNullOrWhiteSpace(cust.CustomerProfileImageUrl)
                ? "/images/default-profile.png"
                : $"{cust.CustomerProfileImageUrl}?v={bust}";

            var vm = new UpdateProfileCusVm
            {
                Name = cust.CustomerFullName,
                UserName = cust.CustomerUserName,
                Email = cust.CustomerEmailAddress,
                PhoneNumber = cust.CustomerPhoneNumber,
                CurrentPhotoUrl = photo
            };

            ViewBag.RewardPoints = cust.CustomerRewardPoints;
            return View("Profile", vm);
        }


        [Authorize(Roles = "Customer")]
        [HttpPost("Profile")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(UpdateProfileCusVm vm)
        {
            var cust = await GetCurrentCustomerAsync();
            if (cust == null) return RedirectToAction(nameof(Login));

            if (!ModelState.IsValid)
            {
                ViewBag.RewardPoints = cust.CustomerRewardPoints;
                return View("Profile", vm);
            }

            // Uniqueness checks (ignore current user)
            var newEmailNorm = vm.Email?.Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(vm.UserName))
            {
                bool takenUser = await _context.Customers
                    .AsNoTracking()
                    .AnyAsync(c => c.CustomerId != cust.CustomerId &&
                                   c.CustomerUserName != null &&
                                   c.CustomerUserName.ToLower() == vm.UserName!.Trim().ToLower());
                if (takenUser)
                    ModelState.AddModelError(nameof(vm.UserName), "Username is already taken.");
            }
            if (!string.IsNullOrWhiteSpace(newEmailNorm))
            {
                bool takenEmail = await _context.Customers
                    .AsNoTracking()
                    .AnyAsync(c => c.CustomerId != cust.CustomerId &&
                                   c.CustomerEmailAddress != null &&
                                   c.CustomerEmailAddress.ToLower() == newEmailNorm);
                if (takenEmail)
                    ModelState.AddModelError(nameof(vm.Email), "CustomerEmailAddress is already in use.");
            }
            if (!ModelState.IsValid)
            {
                ViewBag.RewardPoints = cust.CustomerRewardPoints;
                AddGeneralErrorIfNone();

                return View("Profile", vm);
            }

            // --------- IMAGE HANDLING (Prefer Cropped/Camera, then File) ----------
            string? newPhotoUrl = null;

            // 1) Cropped/camera first (data URL)
            if (!string.IsNullOrWhiteSpace(vm.CapturedImageData))
            {
                var (bytes, ext, err) = FileHelper.ParseDataUrl(vm.CapturedImageData, maxBytes: 2 * 1024 * 1024);
                if (!string.IsNullOrEmpty(err) || bytes == null)
                {
                    ModelState.AddModelError(nameof(vm.ProfileImage), err ?? "Invalid camera/cropped image.");
                    ViewBag.RewardPoints = cust.CustomerRewardPoints;
                    return View("Profile", vm);
                }

                var fileName = FileHelper.SaveBytes(bytes, "images/users", _env.WebRootPath, ext); // ".jpg"
                newPhotoUrl = $"/images/users/{fileName}";
            }
            // 2) File upload fallback
            else if (vm.ProfileImage != null && vm.ProfileImage.Length > 0)
            {
                var error = FileHelper.ValidateImage(vm.ProfileImage);
                if (!string.IsNullOrEmpty(error))
                {
                    ModelState.AddModelError(nameof(vm.ProfileImage), error);
                    ViewBag.RewardPoints = cust.CustomerRewardPoints;
                    return View("Profile", vm);
                }

                var fileName = FileHelper.SaveFile(vm.ProfileImage, "images/users", _env.WebRootPath);
                newPhotoUrl = $"/images/users/{fileName}";
            }
            // ------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(newPhotoUrl))
                cust.CustomerProfileImageUrl = newPhotoUrl;


            // Apply updates
            cust.CustomerFullName = vm.Name?.Trim();
            cust.CustomerUserName = vm.UserName?.Trim();
            cust.CustomerEmailAddress = newEmailNorm;
            cust.CustomerPhoneNumber = vm.PhoneNumber?.Trim();
            if (!string.IsNullOrWhiteSpace(newPhotoUrl))
                cust.CustomerProfileImageUrl = newPhotoUrl;

            _context.Customers.Update(cust);
            await _context.SaveChangesAsync();

            // refresh session + claims with cache-busted photo
            var bust = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var photoForSession = string.IsNullOrWhiteSpace(cust.CustomerProfileImageUrl)
                ? "/images/default-profile.png"
                : $"{cust.CustomerProfileImageUrl}?v={bust}";

            HttpContext.Session.SetString("CustomerId", cust.CustomerId);
            HttpContext.Session.SetString("CustomerUserName", cust.CustomerUserName ?? cust.CustomerFullName ?? "Customer");
            HttpContext.Session.SetString("CustomerPhotoUrl", photoForSession);
            await RefreshAuthAsync(cust);

            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }


        // ----------------------- Helpers -----------------------
        private async Task<Customer?> GetCurrentCustomerAsync()
        {
            // Prefer ClaimTypes.NameIdentifier if present
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? HttpContext.Session.GetString("CustomerId");

            if (string.IsNullOrWhiteSpace(id)) return null;

            return await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == id);
        }

        private async Task RefreshAuthAsync(Customer cust)
        {
            await HttpContext.SignOutAsync("MyCookieAuth");

            var photoUrl = NormalizePhoto(cust.CustomerProfileImageUrl, bust: true);

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, cust.CustomerId),
        new Claim(ClaimTypes.Name, cust.CustomerUserName ?? cust.CustomerFullName ?? "Customer"),
        new Claim(ClaimTypes.Role, "Customer"),
        new Claim("photo", photoUrl)
    };

            var identity = new ClaimsIdentity(claims, "MyCookieAuth");
            await HttpContext.SignInAsync("MyCookieAuth", new ClaimsPrincipal(identity));
        }


        private string NormalizePhoto(string? raw, bool bust = false)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "/images/default-profile.png";

            raw = raw.Trim().Replace('\\', '/');

            // Remove accidental ~/ prefix
            if (raw.StartsWith("~"))
                raw = raw.Substring(1);

            // Remove "wwwroot" from full paths
            if (raw.Contains("wwwroot"))
            {
                int idx = raw.IndexOf("wwwroot") + "wwwroot".Length;
                raw = raw.Substring(idx).TrimStart('/');
            }

            // Remove absolute disk path
            if (raw.Contains(":"))
                raw = Path.GetFileName(raw);

            string path;

            // Data or absolute URL
            if (raw.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                path = raw;
            }
            // Already good: /images/users/abc.jpg
            else if (raw.StartsWith("/"))
            {
                path = raw;
            }
            // Relative folder path: images/users/abc.jpg
            else if (raw.Contains("/"))
            {
                path = "/" + raw.TrimStart('/');
            }
            // Filename ONLY → assume stored in /images/users/
            else
            {
                path = "/images/users/" + raw;
            }

            // Add cache bust
            if (bust &&
                path.StartsWith("/") &&
                !path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var tick = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                path += path.Contains("?") ? $"&v={tick}" : $"?v={tick}";
            }

            return path;
        }

        private string GetRandomGuestAvatarOrDefault()
        {
            try
            {
                var guestDir = Path.Combine(_env.WebRootPath, "images", "guest");

                if (!Directory.Exists(guestDir))
                    return "/images/default-profile.png";

                var files = Directory.GetFiles(guestDir, "*.png");
                if (files.Length == 0)
                    return "/images/default-profile.png";

                var rnd = new Random();
                var pick = files[rnd.Next(files.Length)];
                var fileName = Path.GetFileName(pick);

                return $"/images/guest/{fileName}";
            }
            catch
            {
                // just in case anything fails, fall back to the usual default
                return "/images/default-profile.png";
            }
        }


    }
}
*/
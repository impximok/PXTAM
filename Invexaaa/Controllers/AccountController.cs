/*
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SnomiAssignmentReal.Data;
using SnomiAssignmentReal.Helpers;
using SnomiAssignmentReal.Models;
using SnomiAssignmentReal.Models.ViewModels;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.IO;

namespace SnomiAssignmentReal.Controllers
{
    // Admin/Staff auth + profile
    [Route("Account")]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _cfg;
        private readonly HCaptchaVerifier _hCaptcha;
        private readonly IMemoryCache _cache;

        private const int MAX_BAD_ATTEMPTS = 3;
        private static readonly TimeSpan LOCKOUT_TIME = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan ATTEMPT_TTL = TimeSpan.FromMinutes(10);

        private string AttemptKey(string loginKey, string? ip) => $"login:attempts:admin:{loginKey}:{ip}";
        private string LockKey(string loginKey, string? ip) => $"login:lock:admin:{loginKey}:{ip}";

        public AccountController(
            ApplicationDbContext db,
            IWebHostEnvironment env,
            IConfiguration cfg,
            HCaptchaVerifier hCaptcha,
            IMemoryCache cache)
        {
            _db = db;
            _env = env;
            _cfg = cfg;
            _hCaptcha = hCaptcha;
            _cache = cache;
        }

        // ---------------- LOGIN ----------------
        [AllowAnonymous]
        [HttpGet("Login")]
        public IActionResult Login()
        {
            ViewBag.HCaptchaSiteKey = _cfg["Captcha:hCaptcha:SiteKey"] ?? "";
            return View(new LoginVm());
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVm vm)
        {
            ViewBag.HCaptchaSiteKey = _cfg["Captcha:hCaptcha:SiteKey"] ?? "";
            if (!ModelState.IsValid) return View(vm);

            var loginKey = (vm.Email ?? "").Trim().ToLowerInvariant();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            if (_cache.TryGetValue<DateTimeOffset>(LockKey(loginKey, ip), out var untilUtc) &&
                untilUtc > DateTimeOffset.UtcNow)
            {
                var seconds = (int)(untilUtc - DateTimeOffset.UtcNow).TotalSeconds;
                ModelState.AddModelError(string.Empty, $"Too many failed attempts. Try again in {seconds} seconds.");
                return View(vm);
            }

            var token = Request.Form["h-captcha-response"].ToString();
            var human = await _hCaptcha.VerifyAsync(token, ip);
            if (!human)
            {
                ModelState.AddModelError(string.Empty, "Please complete the hCaptcha challenge.");
                return View(vm);
            }

            var user = _db.Users
                          .Include(u => u.UserRole)
                          .Where(u => u.LoginEmailAddress.ToLower() == loginKey)
                          .OrderByDescending(u => u.PasswordResetTokenExpiry ?? DateTime.MinValue)
                          .ThenByDescending(u => u.UserId)
                          .FirstOrDefault();

            var passwordOk = user != null &&
                             !string.IsNullOrEmpty(user.HashedPassword) &&
                             PasswordHasher.VerifyPassword(vm.Password, user.HashedPassword);

            var roleName = user?.UserRole?.RoleName ?? string.Empty;
            var isPriv = roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                         roleName.Equals("Staff", StringComparison.OrdinalIgnoreCase);

            if (passwordOk && isPriv)
            {
                _cache.Remove(AttemptKey(loginKey, ip));
                _cache.Remove(LockKey(loginKey, ip));

                var photoClaim = NormalizePhoto(user.UserProfileImageUrl, bust: true);

                // ADD THIS so layout can also read from session:
                HttpContext.Session.SetString("CustomerPhotoUrl", photoClaim);

                var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, user!.UserId?.ToString() ?? string.Empty),
    new Claim(ClaimTypes.Name, user.UserFullName ?? string.Empty),
    new Claim(ClaimTypes.Email, user.LoginEmailAddress ?? string.Empty),
    new Claim(ClaimTypes.Role, roleName),
    new Claim("photo", photoClaim)
};



                var identity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    ClaimTypes.Name,
                    ClaimTypes.Role
                );
                await HttpContext.SignInAsync(
                    "MyCookieAuth",
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties
                    {
                        IsPersistent = vm.RememberMe,
                        ExpiresUtc = vm.RememberMe ? DateTime.UtcNow.AddDays(7) : DateTime.UtcNow.AddMinutes(30)
                    });

                return RedirectToAction("Dashboard", "Admin");
            }

            var attemptsKey = AttemptKey(loginKey, ip);
            var attempts = _cache.Get<int?>(attemptsKey) ?? 0;
            attempts++;
            _cache.Set(attemptsKey, attempts, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = ATTEMPT_TTL });

            if (attempts >= MAX_BAD_ATTEMPTS)
            {
                var lockUntil = DateTimeOffset.UtcNow.Add(LOCKOUT_TIME);
                _cache.Set(LockKey(loginKey, ip), lockUntil, lockUntil);
                ModelState.AddModelError(string.Empty, $"Too many failed attempts. Your login is locked for {(int)LOCKOUT_TIME.TotalMinutes} minutes.");
            }
            else
            {
                var left = MAX_BAD_ATTEMPTS - attempts;
                ModelState.AddModelError(nameof(vm.Password), "Invalid email or password.");
                ModelState.AddModelError(string.Empty, $"Login failed. {left} attempt{(left == 1 ? "" : "s")} remaining before lockout.");
            }

            return View(vm);
        }

        // ---------------- LOGOUT ----------------
        [HttpGet("Logout")]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult LogoutConfirm() => View("Logout");

        [HttpPost("Logout")]
        [Authorize(Roles = "Admin,Staff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync("MyCookieAuth");
            return RedirectToAction("Welcome", "Home");
        }

        // ---------------- UPDATE PASSWORD ----------------
        [HttpGet("UpdatePassword")]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult UpdatePassword()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (email is null) return RedirectToAction(nameof(Login));
            return View();
        }

        [HttpPost("UpdatePassword")]
        [Authorize(Roles = "Admin,Staff")]
        [ValidateAntiForgeryToken]
        public IActionResult UpdatePassword(UpdatePasswordVM vm)
        {
            if (!ModelState.IsValid) return View(vm);
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var user = _db.Users.Include(u => u.UserRole).FirstOrDefault(u => u.LoginEmailAddress == email);
            if (user == null)
            {
                TempData["Error"] = "User session expired. Please log in again.";
                return RedirectToAction(nameof(Login));
            }

            if (!PasswordHasher.VerifyPassword(vm.Current, user.HashedPassword))
            {
                ModelState.AddModelError("Current", "Current password is incorrect.");
                return View(vm);
            }
            if (PasswordHasher.VerifyPassword(vm.New, user.HashedPassword))
            {
                ModelState.AddModelError("New", "New password cannot be the same as the current password.");
                return View(vm);
            }

            user.HashedPassword = PasswordHasher.HashPassword(vm.New);
            _db.SaveChanges();

            TempData["Info"] = "HashedPassword updated successfully.";
            return RedirectToAction(nameof(UpdatePassword));
        }

        // ---------------- RESET PASSWORD ----------------
        [HttpGet("ResetPassword")]
        [AllowAnonymous]
        public IActionResult ResetPassword() => View(new ResetPasswordVm());

        [HttpPost("ResetPassword")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(ResetPasswordVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var email = (vm.Email ?? "").Trim().ToLowerInvariant();
            var user = _db.Users.FirstOrDefault(u => u.LoginEmailAddress.ToLower() == email);

            // generic response
            if (user == null)
            {
                TempData["Info"] = "If an account exists, a reset link has been sent to your email.";
                return RedirectToAction(nameof(Login));
            }

            user.PasswordResetToken = Guid.NewGuid().ToString();
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(30);
            _db.SaveChanges();

            SendResetPasswordEmail(user);

            TempData["Info"] = "If an account exists, a reset link has been sent to your email.";
            return RedirectToAction(nameof(Login));
        }

        private void SendResetPasswordEmail(User user)
        {
            var fromAddress = _cfg["CustomerEmailAddress:FromAddress"] ?? "no-reply@snomi.example";
            var fromName = _cfg["CustomerEmailAddress:FromName"] ?? "Snömi Café";
            var supportEmail = _cfg["CustomerEmailAddress:Support"] ?? "support@snomi.example";

            string resetLink = Url.Action("SetNewPassword", "Account",
                new { token = user.PasswordResetToken, email = user.LoginEmailAddress }, Request.Scheme) ?? string.Empty;

            string encName = WebUtility.HtmlEncode(user.UserFullName ?? "User");
            string encLink = WebUtility.HtmlEncode(resetLink);
            string encSupp = WebUtility.HtmlEncode(supportEmail);
            string nowUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'");

            string subject = "HashedPassword Reset — Snömi Café";
            string preheader = "Reset your password with the secure link below. Expires in 30 minutes.";

            string textBody = $@"{subject}

Hello {user.UserFullName},

We received a request to reset your Snömi Café password.

Reset link (valid 30 minutes):
{resetLink}

If you didn’t request this, ignore this message. Your password remains unchanged.

Requested: {nowUtc}
Support: {supportEmail}
";

            var html = new StringBuilder();
            html.Append($@"
<!doctype html>
<html lang=""en"">
<head>
<meta charset=""utf-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<title>{WebUtility.HtmlEncode(subject)}</title>
<style>
body{{margin:0;background:#fff}}.wrap{{padding:32px 16px}}.card{{max-width:640px;margin:0 auto;border:1px solid #e6e6e6;border-radius:14px;padding:32px;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif}}
.brand{{font-weight:600;letter-spacing:.06em;text-transform:uppercase}}.h1{{font-size:28px;font-weight:700;letter-spacing:-.02em}}
.btn{{display:inline-block;background:#000;color:#fff!important;text-decoration:none;padding:14px 22px;border-radius:9999px;font-weight:600}}
.link{{word-break:break-all;font-family:ui-monospace,Menlo,Consolas,monospace;font-size:13px;color:#111}}
.fineprint{{color:#666;font-size:13px}}
</style>
</head>
<body>
<div class=""wrap""><div class=""card"">
<div class=""brand"">SNÖMI CAFÉ</div>
<div class=""h1"">HashedPassword reset</div>
<p>Hello <strong>{encName}</strong>,</p>
<p>We received a request to reset the password for your Snömi Café account.</p>
<p><a class=""btn"" href=""{encLink}"">Reset password</a></p>
<p class=""fineprint""><strong>Expires in 30 minutes.</strong> For security, this link can be used once.</p>
<p>If the button doesn’t work, paste this link:</p>
<p class=""link"">{encLink}</p>
<p class=""fineprint"">Requested: {WebUtility.HtmlEncode(nowUtc)} · Need help? <a href=""mailto:{encSupp}"">{encSupp}</a></p>
</div></div>
</body></html>");

            var mail = new MailMessage
            {
                Subject = subject,
                From = new MailAddress(fromAddress, fromName),
                IsBodyHtml = true
            };
            mail.To.Add(new MailAddress(user.LoginEmailAddress, user.UserFullName ?? user.LoginEmailAddress));
            mail.ReplyToList.Add(new MailAddress(supportEmail, "Snömi Café Support"));
            mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(textBody, Encoding.UTF8, "text/plain"));
            mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html.ToString(), Encoding.UTF8, "text/html"));
            mail.Headers.Add("X-Auto-Response-Suppress", "All");
            mail.Headers.Add("List-Unsubscribe", $"<{encSupp}>");

            EmailHelper.SendEmail(mail, _cfg);
        }

        // ---------------- SET NEW PASSWORD ----------------
        [HttpGet("SetNewPassword")]
        [AllowAnonymous]
        public IActionResult SetNewPassword([FromQuery] string token, [FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Invalid password reset link.";
                return RedirectToAction("ResetPassword");
            }

            var nowUtc = DateTime.UtcNow;
            var normEmail = email.Trim().ToLowerInvariant();

            var user = _db.Users.FirstOrDefault(u =>
                u.LoginEmailAddress.ToLower() == normEmail &&
                u.PasswordResetToken == token);

            if (user == null || user.PasswordResetTokenExpiry is null || user.PasswordResetTokenExpiry < nowUtc)
            {
                TempData["Error"] = "Invalid or expired token.";
                return RedirectToAction("ResetPassword");
            }

            return View(new SetNewPasswordVm { Email = user.LoginEmailAddress, Token = token });
        }

        [HttpPost("SetNewPassword")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult SetNewPassword([FromForm] SetNewPasswordVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var nowUtc = DateTime.UtcNow;
            var normEmail = (vm.Email ?? "").Trim().ToLowerInvariant();

            var user = _db.Users.FirstOrDefault(u =>
                u.LoginEmailAddress.ToLower() == normEmail &&
                u.PasswordResetToken == vm.Token);

            if (user == null || user.PasswordResetTokenExpiry is null || user.PasswordResetTokenExpiry < nowUtc)
            {
                TempData["Error"] = "Invalid or expired token.";
                return RedirectToAction("ResetPassword");
            }

            user.HashedPassword = PasswordHasher.HashPassword(vm.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            _db.SaveChanges();

            TempData["Info"] = "Your password has been updated. Please sign in.";
            return RedirectToAction(nameof(Login));
        }

        // ---------------- UPDATE PROFILE (GET) ----------------
        [HttpGet("UpdateProfile")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> UpdateProfile()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(email)) return RedirectToAction(nameof(Login));

            var user = await _db.Users.AsTracking().FirstOrDefaultAsync(u => u.LoginEmailAddress == email);
            if (user == null) return RedirectToAction(nameof(Login));

            var vm = new UpdateProfileVm
            {
                Email = user.LoginEmailAddress,
                Name = user.UserFullName,
                ProfileImageUrl = user.UserProfileImageUrl
            };
            return View(vm);
        }

        // ---------------- UPDATE PROFILE (POST) ----------------
        [HttpPost("UpdateProfile")]
        [Authorize(Roles = "Admin,Staff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
            UpdateProfileVm vm,
            IFormFile? ProfileImage,
            [FromForm] string? CapturedImageDataUrl)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(email)) return RedirectToAction(nameof(Login));

            var user = await _db.Users.AsTracking().FirstOrDefaultAsync(u => u.LoginEmailAddress == email);
            if (user == null) return RedirectToAction(nameof(Login));

            // Let user save name without re-uploading image
            ModelState.Remove(nameof(vm.ProfileImageUrl));

            // Validate file (if provided)
            if (ProfileImage != null && ProfileImage.Length > 0)
            {
                var err = FileHelper.ValidateImage(ProfileImage);
                if (!string.IsNullOrEmpty(err))
                    ModelState.AddModelError(nameof(vm.ProfileImageUrl), err);
            }

            // Validate data URL (if provided)
            if (!string.IsNullOrWhiteSpace(CapturedImageDataUrl))
            {
                var (_, _, parseErr) = FileHelper.ParseDataUrl(CapturedImageDataUrl, maxBytes: 2 * 1024 * 1024);
                if (!string.IsNullOrEmpty(parseErr))
                    ModelState.AddModelError(nameof(vm.ProfileImageUrl), parseErr);
            }

            if (!ModelState.IsValid)
            {
                vm.Email = user.LoginEmailAddress;
                vm.ProfileImageUrl = user.UserProfileImageUrl;
                return View(vm);
            }

            // Update name
            var newName = (vm.Name ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(newName) && newName != user.UserFullName)
                user.UserFullName = newName;

            // --- PHOTO PRIORITY: Cropped/camera > file upload ---
            if (!string.IsNullOrWhiteSpace(CapturedImageDataUrl))
            {
                var (bytes, ext, parseErr) = FileHelper.ParseDataUrl(CapturedImageDataUrl, maxBytes: 2 * 1024 * 1024);
                if (!string.IsNullOrEmpty(parseErr) || bytes == null)
                {
                    ModelState.AddModelError(nameof(vm.ProfileImageUrl), parseErr ?? "Invalid image.");
                    vm.Email = user.LoginEmailAddress;
                    vm.ProfileImageUrl = user.UserProfileImageUrl;
                    return View(vm);
                }

                FileHelper.DeleteFile(user.UserProfileImageUrl, _env.WebRootPath);
                var fileName = FileHelper.SaveBytes(bytes, "images/users", _env.WebRootPath, ext); // ext like ".jpg"
                user.UserProfileImageUrl = $"/images/users/{fileName}";
            }
            else if (ProfileImage != null && ProfileImage.Length > 0)
            {
                FileHelper.DeleteFile(user.UserProfileImageUrl, _env.WebRootPath);
                var fileName = FileHelper.SaveFile(ProfileImage, "images/users", _env.WebRootPath);
                user.UserProfileImageUrl = $"/images/users/{fileName}";
            }

            await _db.SaveChangesAsync();
            await RefreshAuthAsync(user); // refresh cookie so header avatar updates

            TempData["Info"] = "Profile updated successfully.";
            return RedirectToAction(nameof(UpdateProfile));
        }

        private async Task RefreshAuthAsync(User user)
        {
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "Staff";
            var photo = NormalizePhoto(user.UserProfileImageUrl, bust: true);

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.UserId?.ToString() ?? string.Empty),
        new Claim(ClaimTypes.Name, user.UserFullName ?? string.Empty),
        new Claim(ClaimTypes.Email, user.LoginEmailAddress ?? string.Empty),
        new Claim(ClaimTypes.Role, role),
        new Claim("photo", photo)
    };

            var identity = new ClaimsIdentity(
                claims,
                "MyCookieAuth",
                ClaimTypes.Name,
                ClaimTypes.Role);

            await HttpContext.SignInAsync(
                "MyCookieAuth",
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });
        }




        private string NormalizePhoto(string? raw, bool bust = false)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "/images/default-profile.png";

            raw = raw.Trim().Replace('\\', '/');

            // Remove accidental ~/ prefix
            if (raw.StartsWith("~"))
                raw = raw.Substring(1);

            // Remove "wwwroot" prefix wherever it appears
            if (raw.Contains("wwwroot"))
            {
                int idx = raw.IndexOf("wwwroot") + "wwwroot".Length;
                raw = raw.Substring(idx).TrimStart('/');
            }

            // Remove absolute disk paths e.g. C:/project/wwwroot/images/admin.jpg
            if (raw.Contains(":"))
                raw = Path.GetFileName(raw);

            string path;

            // Absolute or data URLs
            if (raw.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                path = raw;
            }
            // Already app path: /images/admin.jpg
            else if (raw.StartsWith("/"))
            {
                path = raw;
            }
            // Folder + name: images/admin.jpg
            else if (raw.Contains("/"))
            {
                path = "/" + raw.TrimStart('/');
            }
            // Only filename → admin.jpg → assume /images/
            else
            {
                path = "/images/" + raw;
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


    }
}
*/
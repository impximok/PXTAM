using Invexaaa.Data;
using Invexaaa.Helpers; // <-- hCaptcha verifier
using Invexaaa.Models.Invexa;
using Invexaaa.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Net.Mail;
using System.Security.Claims;

namespace Invexaaa.Controllers
{
    [Route("User")]
    public class UserController : Controller
    {
        private readonly InvexaDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly HCaptchaVerifier _hCaptcha;

        // ===== Lockout Settings =====
        private const int MAX_BAD_ATTEMPTS = 3;
        private static readonly TimeSpan LOCKOUT_TIME = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan ATTEMPT_TTL = TimeSpan.FromMinutes(10);

        private string AttemptKey(string key, string ip) => $"login:attempts:{key}:{ip}";
        private string LockKey(string key, string ip) => $"login:lock:{key}:{ip}";

        public UserController(
            InvexaDbContext context,
            IMemoryCache cache,
            IConfiguration configuration,
            HCaptchaVerifier hCaptcha)
        {
            _context = context;
            _cache = cache;
            _configuration = configuration;
            _hCaptcha = hCaptcha;
        }

        [AllowAnonymous]
        [HttpGet("Register")]
        public IActionResult Register()
        {
            ViewBag.HCaptchaSiteKey = _configuration["Captcha:hCaptcha:SiteKey"];
            return View();
        }

        [AllowAnonymous]
        [HttpPost("Register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            ViewBag.HCaptchaSiteKey = _configuration["Captcha:hCaptcha:SiteKey"];

            // ===== hCaptcha =====
            var token = Request.Form["h-captcha-response"].ToString();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            if (string.IsNullOrWhiteSpace(token) ||
                !await _hCaptcha.VerifyAsync(token, ip))
            {
                ModelState.AddModelError(string.Empty, "Captcha verification failed.");
                return View(model);
            }

            if (!ModelState.IsValid)
                return View(model);

            // ===== Email exists =====
            bool exists = await _context.Users
                .AnyAsync(u => u.UserEmail == model.UserEmail.ToLower());

            if (exists)
            {
                ModelState.AddModelError(nameof(model.UserEmail), "Email already exists.");
                return View(model);
            }

            /* ================= IMAGE HANDLING ================= */

            string? imagePath = null;

            // 📸 Camera image (base64)
            if (!string.IsNullOrWhiteSpace(model.CapturedImageData))
            {
                var base64 = model.CapturedImageData.Split(',')[1];
                var bytes = Convert.FromBase64String(base64);

                var fileName = $"{Guid.NewGuid()}.jpg";
                var savePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/images/users",
                    fileName
                );

                await System.IO.File.WriteAllBytesAsync(savePath, bytes);
                imagePath = $"/images/users/{fileName}";
            }
            // 📁 Uploaded file
            else if (model.ProfileImage != null)
            {
                var ext = Path.GetExtension(model.ProfileImage.FileName);
                var fileName = $"{Guid.NewGuid()}{ext}";
                var savePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/images/users",
                    fileName
                );

                using var stream = new FileStream(savePath, FileMode.Create);
                await model.ProfileImage.CopyToAsync(stream);

                imagePath = $"/images/users/{fileName}";
            }

            /* ================= SAVE USER ================= */

            var user = new User
            {
                UserFullName = model.UserFullName,
                UserEmail = model.UserEmail.ToLower(),
                UserPhone = model.UserPhone,
                UserRole = model.UserRole,
                UserStatus = "Active",
                UserPasswordHash = PasswordHasher.HashPassword(model.Password),
                UserProfileImageUrl = imagePath,
                UserCreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Account created successfully. Please log in.";
            return RedirectToAction("Login");
        }


        // ========================= LOGIN =========================
        [AllowAnonymous]
        [HttpGet("Login")]
        public IActionResult Login()
        {
            ViewBag.HCaptchaSiteKey = _configuration["Captcha:hCaptcha:SiteKey"];
            return View();
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            ViewBag.HCaptchaSiteKey = _configuration["Captcha:hCaptcha:SiteKey"];

            var loginKey = email.Trim().ToLower();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // ===== hCAPTCHA VERIFY =====
            var token = Request.Form["h-captcha-response"].ToString();
            if (string.IsNullOrWhiteSpace(token) ||
                !await _hCaptcha.VerifyAsync(token, ip))
            {
                ModelState.AddModelError(string.Empty, "Captcha verification failed. Please try again.");
                return View();
            }

            // ---- LOCKOUT CHECK ----
            if (_cache.TryGetValue<DateTimeOffset>(LockKey(loginKey, ip), out var lockedUntil)
                && lockedUntil > DateTimeOffset.UtcNow)
            {
                ModelState.AddModelError(string.Empty, "Too many failed attempts. Please try again later.");
                return View();
            }

            // ---- USER LOOKUP ----
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserEmail.ToLower() == loginKey);

            bool passwordOk = user != null &&
                BCrypt.Net.BCrypt.Verify(password, user.UserPasswordHash);

            if (passwordOk)
            {
                if (user!.UserStatus != "Active")
                {
                    ModelState.AddModelError(string.Empty, "Your account is inactive.");
                    return View();
                }

                // clear lockout counters
                _cache.Remove(AttemptKey(loginKey, ip));
                _cache.Remove(LockKey(loginKey, ip));

                // ===============================
                // PATCH: SESSION FOR _Layout.cshtml
                // ===============================
                HttpContext.Session.SetString("UserFullName", user.UserFullName);

                if (!string.IsNullOrWhiteSpace(user.UserProfileImageUrl))
                {
                    HttpContext.Session.SetString(
                        "UserProfilePhoto",
                        user.UserProfileImageUrl
                    );
                }
                else
                {
                    HttpContext.Session.Remove("UserProfilePhoto");
                }

                // ===============================
                // PATCH: CLAIMS (ADD PHOTO)
                // ===============================
                var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
        new Claim(ClaimTypes.Name, user.UserFullName),
        new Claim(ClaimTypes.Email, user.UserEmail),
        new Claim(ClaimTypes.Role, user.UserRole),
        new Claim("photo", user.UserProfileImageUrl ?? "")
    };

                var identity = new ClaimsIdentity(claims, "MyCookieAuth");

                await HttpContext.SignInAsync(
                    "MyCookieAuth",
                    new ClaimsPrincipal(identity)
                );

                return RedirectToAction("Index", "Dashboard");
            }


            // ---- FAILED LOGIN ----
            var attemptsKey = AttemptKey(loginKey, ip);
            var attempts = _cache.Get<int?>(attemptsKey) ?? 0;
            attempts++;

            _cache.Set(attemptsKey, attempts,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ATTEMPT_TTL
                });

            if (attempts >= MAX_BAD_ATTEMPTS)
            {
                var lockUntil = DateTimeOffset.UtcNow.Add(LOCKOUT_TIME);
                _cache.Set(LockKey(loginKey, ip), lockUntil, lockUntil);
                ModelState.AddModelError(string.Empty, "Account locked for 2 minutes.");
            }
            else
            {
                ModelState.AddModelError(string.Empty,
                    $"Invalid login. {MAX_BAD_ATTEMPTS - attempts} attempt(s) left.");
            }

            return View();
        }

        // ========================= LOGOUT =========================
        [Authorize]
        [HttpPost("Logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookieAuth");
            return RedirectToAction("Login");
        }

        // ========================= ACCESS DENIED =========================
        [HttpGet("AccessDenied")]
        public IActionResult AccessDenied()
        {
            return View();
        }
        [AllowAnonymous]
        [HttpGet("ForgotPassword")]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }
        [AllowAnonymous]
        [HttpPost("ForgotPassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var email = vm.Email.Trim().ToLower();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserEmail.ToLower() == email);

            // 🔐 Generic response (security)
            if (user == null)
            {
                TempData["Info"] =
                    "If the email is registered in our system, a password reset link has been sent.";
                return RedirectToAction("Login");
            }

            user.PasswordResetToken = Guid.NewGuid().ToString();
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(30);

            await _context.SaveChangesAsync();

            SendResetPasswordEmail(user);

            TempData["Info"] =
                "If the email is registered in our system, a password reset link has been sent.";
            return RedirectToAction("Login");
        }


        [AllowAnonymous]
        [HttpGet("ResetPassword")]
        public async Task<IActionResult> ResetPassword(string token, string email)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
                return RedirectToAction("ForgotPassword");

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.UserEmail.ToLower() == email.ToLower() &&
                u.PasswordResetToken == token &&
                u.PasswordResetTokenExpiry > DateTime.UtcNow
            );

            if (user == null)
            {
                TempData["Error"] = "Invalid or expired reset link.";
                return RedirectToAction("ForgotPassword");
            }

            return View(new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            });
        }

        [AllowAnonymous]
        [HttpPost("ResetPassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.UserEmail.ToLower() == vm.Email.ToLower() &&
                u.PasswordResetToken == vm.Token &&
                u.PasswordResetTokenExpiry > DateTime.UtcNow
            );

            if (user == null)
            {
                TempData["Error"] = "Invalid or expired reset link.";
                return RedirectToAction("ForgotPassword");
            }

            user.UserPasswordHash = PasswordHasher.HashPassword(vm.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Password reset successful. Please log in.";
            return RedirectToAction("Login");
        }

        private void SendResetPasswordEmail(User user)
        {
            var resetLink = Url.Action(
                "ResetPassword",
                "User",
                new { token = user.PasswordResetToken, email = user.UserEmail },
                Request.Scheme
            );

            var mail = new MailMessage
            {
                Subject = "Reset Your Password",
                Body = $@"
Hello {user.UserFullName},

We received a request to reset your password.

Reset link (valid for 30 minutes):
{resetLink}

If you did not request this, please ignore this email.",
                IsBodyHtml = false
            };

            mail.To.Add(user.UserEmail);

            EmailHelper.SendEmail(mail, _configuration);
        }

        [Authorize]
        [HttpGet("UpdateProfile")]
        public async Task<IActionResult> UpdateProfile()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction("Login");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserEmail == email);

            if (user == null)
                return RedirectToAction("Login");

            var vm = new UpdateProfileViewModel
            {
                Email = user.UserEmail,
                Name = user.UserFullName,
                ProfileImageUrl = user.UserProfileImageUrl
            };

            return View(vm);
        }
        [Authorize]
        [HttpPost("UpdateProfile")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
    UpdateProfileViewModel vm,
    IFormFile? ProfileImage,
    [FromForm] string? CapturedImageDataUrl)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction("Login");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserEmail == email);

            if (user == null)
                return RedirectToAction("Login");

            // remove readonly email validation
            ModelState.Remove(nameof(vm.Email));

            if (!ModelState.IsValid)
            {
                vm.ProfileImageUrl = user.UserProfileImageUrl;
                return View(vm);
            }

            // update name
            user.UserFullName = vm.Name.Trim();

            /* ===== IMAGE PRIORITY =====
               1. Camera / cropped image
               2. Uploaded file
            */

            if (!string.IsNullOrWhiteSpace(CapturedImageDataUrl))
            {
                var base64 = CapturedImageDataUrl.Split(',')[1];
                var bytes = Convert.FromBase64String(base64);

                var fileName = $"{Guid.NewGuid()}.jpg";
                var savePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/images/users",
                    fileName
                );

                await System.IO.File.WriteAllBytesAsync(savePath, bytes);

                user.UserProfileImageUrl = $"/images/users/{fileName}";
            }
            else if (ProfileImage != null && ProfileImage.Length > 0)
            {
                var ext = Path.GetExtension(ProfileImage.FileName);
                var fileName = $"{Guid.NewGuid()}{ext}";
                var savePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/images/users",
                    fileName
                );

                using var stream = new FileStream(savePath, FileMode.Create);
                await ProfileImage.CopyToAsync(stream);

                user.UserProfileImageUrl = $"/images/users/{fileName}";
            }

            await _context.SaveChangesAsync();
            // ===============================
            // PATCH: REFRESH SESSION AFTER PROFILE UPDATE
            // ===============================
            HttpContext.Session.SetString("UserFullName", user.UserFullName);

            if (!string.IsNullOrWhiteSpace(user.UserProfileImageUrl))
            {
                HttpContext.Session.SetString(
                    "UserProfilePhoto",
                    user.UserProfileImageUrl
                );
            }
            else
            {
                HttpContext.Session.Remove("UserProfilePhoto");
            }

            TempData["Info"] = "Profile updated successfully.";


            return RedirectToAction(nameof(UpdateProfile));
        }


    }

}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using Invexaaa.Data;
using Invexaaa.Models.Invexa;
using Invexaaa.Helpers; // <-- hCaptcha verifier
using Microsoft.Extensions.Configuration;

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

                // clear attempts
                _cache.Remove(AttemptKey(loginKey, ip));
                _cache.Remove(LockKey(loginKey, ip));

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                    new Claim(ClaimTypes.Name, user.UserFullName),
                    new Claim(ClaimTypes.Email, user.UserEmail),
                    new Claim(ClaimTypes.Role, user.UserRole)
                };

                var identity = new ClaimsIdentity(claims, "MyCookieAuth");
                await HttpContext.SignInAsync("MyCookieAuth", new ClaimsPrincipal(identity));

                return user.UserRole switch
                {
                    "Admin" => RedirectToAction("Index", "Dashboard"),
                    "Manager" => RedirectToAction("Index", "Dashboard"),
                    "Staff" => RedirectToAction("Index", "Dashboard"),
                    _ => RedirectToAction("Login")
                };
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
    }
}

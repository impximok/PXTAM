using Invexaaa.Helpers;
using Invexaaa.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Security.Claims;

namespace Invexaaa.Controllers
{
    [Authorize]
    public class FeedbackController : Controller
    {
        private readonly IConfiguration _config;

        public FeedbackController(IConfiguration config)
        {
            _config = config;
        }

        // =============================
        // FEEDBACK FORM
        // =============================
        public IActionResult CreateFeedback()
        {
            return View();
        }

        // =============================
        // SEND FEEDBACK EMAIL
        // =============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateFeedback (FeedbackEmailViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var userEmail =
    User.FindFirstValue(ClaimTypes.Email)
    ?? User.Identity?.Name
    ?? "Unknown user";


            var body =
$@"
<div style='font-family: Arial, sans-serif; color:#000; max-width:600px;'>

  <h3 style='font-weight:600; border-bottom:1px solid #000; padding-bottom:6px;'>
    Invexa — System Feedback
  </h3>

  <p><strong>Submitted By</strong></p>
  <p style='margin-left:10px;'>
    Email: {userEmail}
  </p>

  <p><strong>Message</strong></p>
  <div style='border-left:3px solid #000; padding-left:12px;'>
    {vm.Message.Replace("\n", "<br/>")}
  </div>

  <hr style='border:1px solid #000; margin-top:20px;' />

  <p style='font-size:12px;'>
    Submitted on {DateTime.Now:dd MMM yyyy HH:mm}<br/>
    Invexa Smart Inventory Intelligence System
  </p>

</div>
";


            var mail = new MailMessage
            {
                Subject = $"[Invexa Feedback] {vm.Subject}",
                Body = body,
                IsBodyHtml = true
            };

            // Send to system owner (SMTP User)
            mail.To.Add(_config["Smtp:User"]!);

            EmailHelper.SendEmail(mail, _config);

            TempData["Success"] = "Thank you for your feedback. It has been sent successfully.";
            return RedirectToAction("CreateFeedback", "Feedback");
        }
    }
}

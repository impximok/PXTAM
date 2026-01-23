using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace Invexaaa.Helpers
{
    public static class EmailHelper
    {
        public static void SendEmail(MailMessage mail, IConfiguration cfg)
        {
            var s = cfg.GetSection("Smtp");

            var user = s["User"] ?? throw new Exception("Missing Smtp:User");
            var pass = s["Pass"] ?? throw new Exception("Missing Smtp:Pass");
            var host = s["Host"] ?? "smtp.gmail.com";
            var port = int.TryParse(s["Port"], out var p) ? p : 587;
            var enableSsl = bool.TryParse(s["EnableSsl"], out var ssl) ? ssl : true;
            var fromName = s["Name"] ?? "Invexa";

            // Ensure From is always set
            mail.From ??= new MailAddress(user, fromName);

            using var smtp = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(user, pass),
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            smtp.Send(mail);
        }
    }
}

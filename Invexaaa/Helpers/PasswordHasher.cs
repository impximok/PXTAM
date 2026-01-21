using BCrypt.Net;
using System.Net;
using System.Net.Mail;

namespace Invexaaa.Helpers;

public static class PasswordHasher
{
    // Hash the password securely
    public static string HashPassword(string plainPassword)
    {
        return BCrypt.Net.BCrypt.HashPassword(plainPassword);
    }

    // Verify a plain password against a hashed password
    public static bool VerifyPassword(string plainPassword, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);
    }

    public static string GenerateRandomPassword()
    {
        return "Snomi@" + Guid.NewGuid().ToString("N")[..6];
    }

    public static void SendEmail(MailMessage message)
    {
        using var smtp = new SmtpClient("smtp.yourhost.com")
        {
            Port = 587,
            Credentials = new NetworkCredential("yourEmail", "yourPassword"),
            EnableSsl = true
        };

        smtp.Send(message);
    }

}

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Invexaaa.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required, MaxLength(100)]
        [Display(Name = "Full Name")]
        public string UserFullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(100)]
        public string UserEmail { get; set; } = string.Empty;

        [Required, MinLength(8)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string UserPhone { get; set; } = string.Empty;

        [Required]
        public string UserRole { get; set; } = "Staff";

        /* ================= PROFILE IMAGE ================= */

        // Uploaded file
        public IFormFile? ProfileImage { get; set; }

        // Camera capture (base64)
        public string? CapturedImageData { get; set; }
    }
}

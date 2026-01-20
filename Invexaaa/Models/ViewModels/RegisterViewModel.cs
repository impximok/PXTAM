using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
namespace SnomiAssignmentReal.Models.ViewModels
{
    using SnomiAssignmentReal.Validation;

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "CategoryName is required")]
        [MaxLength(50)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [MaxLength(50)]
        public string UserName { get; set; }

        [Required(ErrorMessage = "CustomerEmailAddress is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [PasswordComplexity]
        [MaxLength(255)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }


        [Phone(ErrorMessage = "Invalid phone number")]
        [MaxLength(15)]
        public string PhoneNumber { get; set; }

        // Traditional file upload (still supported)
        public IFormFile? ProfileImage { get; set; }

        // NEW: Base64 data URL from camera (e.g., "data:image/jpeg;base64,...")
        public string? CapturedImageData { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.Invexa
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [MaxLength(100, ErrorMessage = "Full name must not exceed 100 characters.")]
        public string UserFullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [MaxLength(100, ErrorMessage = "Email must not exceed 100 characters.")]
        public string UserEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        public string UserPasswordHash { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [MaxLength(20, ErrorMessage = "Phone number must not exceed 20 characters.")]
        public string UserPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "User role is required.")]
        [MaxLength(20, ErrorMessage = "User role must not exceed 20 characters.")]
        public string UserRole { get; set; } = "Staff";
        // Admin / Manager / Staff

        [Required(ErrorMessage = "User status is required.")]
        [MaxLength(20, ErrorMessage = "User status must not exceed 20 characters.")]
        public string UserStatus { get; set; } = "Active";

        [MaxLength(255, ErrorMessage = "Profile image URL must not exceed 255 characters.")]
        public string? UserProfileImageUrl { get; set; }

        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }

        [Required(ErrorMessage = "User creation date is required.")]
        public DateTime UserCreatedAt { get; set; } = DateTime.Now;
    }
}

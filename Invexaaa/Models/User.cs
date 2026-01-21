using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SnomiAssignmentReal.Validation;


namespace SnomiAssignmentReal.Models;

public class User
{
    [Key]
    [Required(ErrorMessage = "User ID is required.")]
    [MaxLength(10, ErrorMessage = "User ID cannot exceed 10 characters.")]
    public string UserId { get; set; } // Unique ID for each user (e.g., "U001")

    [Required(ErrorMessage = "RoleName is required.")]
    [MaxLength(100, ErrorMessage = "RoleName cannot exceed 100 characters.")]
    public string UserFullName { get; set; } // Staff/Admin name
     
    [MaxLength(255)]
    public string? UserProfileImageUrl { get; set; } // Optional

    [Required(ErrorMessage = "CustomerEmailAddress is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [MaxLength(100, ErrorMessage = "CustomerEmailAddress cannot exceed 100 characters.")]
    public string LoginEmailAddress { get; set; } // Login email

    [Required(ErrorMessage = "HashedPassword is required.")]
    [MaxLength(255, ErrorMessage = "HashedPassword cannot exceed 255 characters.")]
    [PasswordComplexity]
    public string HashedPassword { get; set; } // Hashed password

    [Required(ErrorMessage = "User Role is required.")]
    public string UserRoleId { get; set; } // Foreign key to UserRole

    // Navigation property to UserRole
    public UserRole UserRole { get; set; }

    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

}


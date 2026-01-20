using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnomiAssignmentReal.Models;

public class UserRole
{
    [Key]
    [Required(ErrorMessage = "User Role ID is required.")]
    [MaxLength(10, ErrorMessage = "User Role ID cannot exceed 10 characters.")]
    public string UserRoleId { get; set; } // e.g., "R100"

    [Required(ErrorMessage = "Role name is required.")]
    [MaxLength(50, ErrorMessage = "Role name cannot exceed 50 characters.")]
    public string RoleName { get; set; } // e.g., "Admin", "Staff"

    [MaxLength(255, ErrorMessage = "CategoryDescription cannot exceed 255 characters.")]
    public string? RoleDescription { get; set; } // Optional: e.g., "Can manage users and settings"

    // Navigation properties

    // One role can be assigned to many users
    public ICollection<User>? AssignedUsers { get; set; }

    // One role can have access to many functions (via join table)
    public ICollection<UserRoleFunction>? RoleFunctionPermissions { get; set; }
}

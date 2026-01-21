using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnomiAssignmentReal.Models;

public class UserRoleFunction
{
    [Key]
    public string UserRoleFunctionId { get; set; } // Unique ID like "URF100"

    [Required(ErrorMessage = "User Role ID is required.")]
    [MaxLength(10, ErrorMessage = "User Role ID cannot exceed 10 characters.")]
    public string UserRoleId { get; set; }
    public UserRole UserRole { get; set; } // Navigation

    [Required(ErrorMessage = "Function ID is required.")]
    [MaxLength(10, ErrorMessage = "Function ID cannot exceed 10 characters.")]
    public string FunctionId { get; set; }
    public Function Function { get; set; } // Navigation

    public bool IsFunctionEnabledForRole { get; set; } = true; // Whether the function is active for this role
}

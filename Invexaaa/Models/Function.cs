using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnomiAssignmentReal.Models;

public class Function
{
    [Key]
    [MaxLength(10)]
    public string FunctionId { get; set; } // e.g., "F100"

    [Required(ErrorMessage = "Function name is required.")]
    [MaxLength(100, ErrorMessage = "Function name cannot exceed 100 characters.")]
    public string FunctionName { get; set; } // e.g., "Manage Menu"

    [MaxLength(255, ErrorMessage = "CategoryDescription cannot exceed 255 characters.")]
    public string FunctionDescription { get; set; }

    // Navigation property: one function can be mapped to many UserRoleFunction records
    public ICollection<UserRoleFunction> FunctionRoleMappings { get; set; }
}

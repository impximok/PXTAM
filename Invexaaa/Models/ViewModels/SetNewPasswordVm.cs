using System.ComponentModel.DataAnnotations;
using SnomiAssignmentReal.Validation;

namespace SnomiAssignmentReal.Models.ViewModels;

public class SetNewPasswordVm
{
    public string Email { get; set; }
    public string Token { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [PasswordComplexity]
    [StringLength(100, MinimumLength = 5)]
    public string NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare("NewPassword")]
    public string ConfirmPassword { get; set; }
}

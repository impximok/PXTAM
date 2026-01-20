using System.ComponentModel.DataAnnotations;

namespace SnomiAssignmentReal.Models.ViewModels;

public class LoginVm
{
    [Required(ErrorMessage = "CustomerEmailAddress is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "HashedPassword is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    public bool RememberMe { get; set; }
}
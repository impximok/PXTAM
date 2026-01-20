using System.ComponentModel.DataAnnotations;

namespace SnomiAssignmentReal.Models.ViewModels
{
    public class ResetPasswordVm
    {

        [Required(ErrorMessage = "CustomerEmailAddress is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [MaxLength(50, ErrorMessage = "CustomerEmailAddress cannot exceed 50 characters.")]
        public string Email { get; set; }
    }
}

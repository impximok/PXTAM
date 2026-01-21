using System.ComponentModel.DataAnnotations;

namespace SnomiAssignmentReal.Models.ViewModels
{
    public class EmailVm
    {
        [Required(ErrorMessage = "CustomerEmailAddress is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [MaxLength(100, ErrorMessage = "CustomerEmailAddress must not exceed 100 characters.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Subject is required.")]
        [MaxLength(150, ErrorMessage = "Subject must not exceed 150 characters.")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "CustomerEmailAddress body is required.")]
        public string Body { get; set; }

        public bool IsBodyHtml { get; set; } = true;
    }
}

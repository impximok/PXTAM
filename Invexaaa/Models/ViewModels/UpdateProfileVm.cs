using System.ComponentModel.DataAnnotations;

namespace SnomiAssignmentReal.Models.ViewModels
{
    public class UpdateProfileVm
    {
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "CategoryName cannot exceed 100 characters.")]
        public string Name { get; set; }

        public string? ProfileImageUrl { get; set; }

        public string? CapturedImageDataUrl { get; set; } // data:image/jpeg;base64,...


    }
}

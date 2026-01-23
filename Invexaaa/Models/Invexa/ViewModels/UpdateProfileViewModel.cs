using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.ViewModels
{
    public class UpdateProfileViewModel
    {
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // existing profile image (for preview)
        public string? ProfileImageUrl { get; set; }

        // file upload
        public IFormFile? ProfileImage { get; set; }

        // camera / cropped image
        public string? CapturedImageDataUrl { get; set; }
    }
}

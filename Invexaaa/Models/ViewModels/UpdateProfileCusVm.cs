using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SnomiAssignmentReal.Models.ViewModels
{
    public class UpdateProfileCusVm
    {
        [Required, MaxLength(50)]
        public string? Name { get; set; }

        [Required, MaxLength(50)]
        public string? UserName { get; set; }

        [Required, EmailAddress, MaxLength(100)]
        public string? Email { get; set; }

        [Phone, MaxLength(15)]
        public string? PhoneNumber { get; set; }

        // Display-only current photo URL
        public string? CurrentPhotoUrl { get; set; }

        // Standard file upload
        public IFormFile? ProfileImage { get; set; }

        // ⬅️ NEW: camera snapshot as data URL (e.g., "data:image/jpeg;base64,...")
        public string? CapturedImageData { get; set; }
    }
}

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Invexaaa.Models.ViewModels
{
    public class UpdateProfileViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [MaxLength(254, ErrorMessage = "Email is too long.")]
        public string Email { get; set; } = string.Empty;


        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be 2–100 characters.")]
        public string Name { get; set; } = string.Empty;

        // existing profile image (for preview)
        public string? ProfileImageUrl { get; set; }

        // file upload
        public IFormFile? ProfileImage { get; set; }

        // camera / cropped image (data URL)
        public string? CapturedImageDataUrl { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // ✅ Name cannot be just spaces
            if (!string.IsNullOrWhiteSpace(Name))
            {
                Name = Name.Trim();

                // Optional: allow letters, numbers, spaces, dot, dash, apostrophe
                // (prevents weird symbols in display name)
                var ok = Regex.IsMatch(Name, @"^[\p{L}\p{N} .'\-]+$");
                if (!ok)
                {
                    yield return new ValidationResult(
                        "Name can only contain letters, numbers, spaces, . ' -",
                        new[] { nameof(Name) }
                    );
                }
            }

            // ✅ Validate file if user chose file upload
            if (ProfileImage != null && ProfileImage.Length > 0)
            {
                const long maxBytes = 2 * 1024 * 1024; // 2MB
                if (ProfileImage.Length > maxBytes)
                {
                    yield return new ValidationResult(
                        "Image must be 2MB or less.",
                        new[] { nameof(ProfileImage) }
                    );
                }

                var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(ProfileImage.FileName)?.ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(ext) || !allowedExt.Contains(ext))
                {
                    yield return new ValidationResult(
                        "Only JPG, PNG, or WEBP images are allowed.",
                        new[] { nameof(ProfileImage) }
                    );
                }

                // Optional: check content-type too (not perfect but helps)
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
                if (!allowedTypes.Contains(ProfileImage.ContentType))
                {
                    yield return new ValidationResult(
                        "Invalid image format.",
                        new[] { nameof(ProfileImage) }
                    );
                }
            }

            // ✅ Validate captured data url if user used editor/camera
            if (!string.IsNullOrWhiteSpace(CapturedImageDataUrl))
            {
                // basic "data:image/...;base64," check
                if (!CapturedImageDataUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                {
                    yield return new ValidationResult(
                        "Captured image is invalid.",
                        new[] { nameof(CapturedImageDataUrl) }
                    );
                }

                // rough size check (Base64 ~ 4/3 size)
                // If you want strict, validate in controller when converting base64 -> bytes.
                if (CapturedImageDataUrl.Length > 4_000_000) // ~3MB-ish string cap
                {
                    yield return new ValidationResult(
                        "Captured image is too large.",
                        new[] { nameof(CapturedImageDataUrl) }
                    );
                }
            }

            // ✅ If you want to REQUIRE image change, uncomment this:
            // bool hasUpload = ProfileImage != null && ProfileImage.Length > 0;
            // bool hasCapture = !string.IsNullOrWhiteSpace(CapturedImageDataUrl);
            // if (!hasUpload && !hasCapture)
            // {
            //     yield return new ValidationResult(
            //         "Please upload a profile image or use the camera.",
            //         new[] { nameof(ProfileImage), nameof(CapturedImageDataUrl) }
            //     );
            // }

            // ✅ If both provided, prefer Captured and warn user OR block:
            if (ProfileImage != null && ProfileImage.Length > 0 && !string.IsNullOrWhiteSpace(CapturedImageDataUrl))
            {
                yield return new ValidationResult(
                    "Please use either Upload OR Camera, not both.",
                    new[] { nameof(ProfileImage), nameof(CapturedImageDataUrl) }
                );
            }
        }
    }
}

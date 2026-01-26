using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Invexaaa.Models.ViewModels
{
    // ✅ Rule: must provide either upload OR camera capture
    [RequireProfilePhoto]
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [MaxLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
        [Display(Name = "Full Name")]
        public string UserFullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email Address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        public string UserEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        [RegularExpression(
            @"^(?=.*[A-Z])(?=.*[\W_]).{8,}$",
            ErrorMessage = "Password must contain at least one uppercase letter and one special character."
        )]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;


        [Required(ErrorMessage = "Confirm Password is required.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Confirm Password does not match Password.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone Number is required.")]
        [MaxLength(20, ErrorMessage = "Phone Number cannot exceed 20 characters.")]
        [RegularExpression(@"^[0-9+\-\s]{7,20}$", ErrorMessage = "Phone Number format is invalid.")]
        public string UserPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required.")]
        [RegularExpression(@"^(Staff|Manager|Admin)$", ErrorMessage = "Invalid role selected.")]
        public string UserRole { get; set; } = "";

        /* ================= PROFILE IMAGE ================= */

        // ✅ Uploaded file validation
        [MaxFileSize(2 * 1024 * 1024, ErrorMessage = "Image must be 2MB or less.")]
        [AllowedExtensions(new[] { ".jpg", ".jpeg", ".png", ".gif" }, ErrorMessage = "Only JPG, PNG, or GIF is allowed.")]
        public IFormFile? ProfileImage { get; set; }

        // ✅ Camera capture (base64) validation
        [MaxBase64Size(2 * 1024 * 1024, ErrorMessage = "Captured image must be 2MB or less.")]
        [AllowedDataUrlMime(new[] { "image/jpeg", "image/png", "image/gif" }, ErrorMessage = "Captured image must be JPG, PNG, or GIF.")]
        public string? CapturedImageData { get; set; }
    }

    // ============================
    // ✅ Custom Validators
    // ============================

    public class RequireProfilePhotoAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var model = (RegisterViewModel)validationContext.ObjectInstance;

            var hasUpload = model.ProfileImage != null && model.ProfileImage.Length > 0;
            var hasCapture = !string.IsNullOrWhiteSpace(model.CapturedImageData);

            if (!hasUpload && !hasCapture)
            {
                return new ValidationResult(
                    "Please upload a profile photo or capture using Camera.",
                    new[] { nameof(RegisterViewModel.ProfileImage) }
                );
            }

            return ValidationResult.Success;
        }
    }

    public class MaxFileSizeAttribute : ValidationAttribute
    {
        private readonly int _maxBytes;
        public MaxFileSizeAttribute(int maxBytes) => _maxBytes = maxBytes;

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not IFormFile file || file.Length == 0) return ValidationResult.Success;
            return file.Length > _maxBytes ? new ValidationResult(ErrorMessage) : ValidationResult.Success;
        }
    }

    public class AllowedExtensionsAttribute : ValidationAttribute
    {
        private readonly string[] _extensions;
        public AllowedExtensionsAttribute(string[] extensions)
        {
            _extensions = extensions.Select(x => x.ToLowerInvariant()).ToArray();
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not IFormFile file || file.Length == 0) return ValidationResult.Success;

            var fileName = file.FileName ?? "";
            var dot = fileName.LastIndexOf('.');
            var ext = dot >= 0 ? fileName.Substring(dot).ToLowerInvariant() : "";

            return _extensions.Contains(ext)
                ? ValidationResult.Success
                : new ValidationResult(ErrorMessage);
        }
    }

    public class AllowedDataUrlMimeAttribute : ValidationAttribute
    {
        private readonly string[] _mimes;
        public AllowedDataUrlMimeAttribute(string[] mimes)
        {
            _mimes = mimes.Select(x => x.ToLowerInvariant()).ToArray();
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var s = value as string;
            if (string.IsNullOrWhiteSpace(s)) return ValidationResult.Success;

            // Expect: data:image/png;base64,xxxx
            if (!s.StartsWith("data:", StringComparison.OrdinalIgnoreCase) || !s.Contains(";base64,"))
                return new ValidationResult("Invalid captured image format.");

            var mime = s.Substring(5, s.IndexOf(";base64,", StringComparison.OrdinalIgnoreCase) - 5).ToLowerInvariant();
            return _mimes.Contains(mime)
                ? ValidationResult.Success
                : new ValidationResult(ErrorMessage);
        }
    }

    public class MaxBase64SizeAttribute : ValidationAttribute
    {
        private readonly int _maxBytes;
        public MaxBase64SizeAttribute(int maxBytes) => _maxBytes = maxBytes;

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var s = value as string;
            if (string.IsNullOrWhiteSpace(s)) return ValidationResult.Success;

            var idx = s.IndexOf(";base64,", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return new ValidationResult("Invalid captured image format.");

            var b64 = s[(idx + ";base64,".Length)..];

            // bytes ≈ (len * 3) / 4 - padding
            var len = b64.Length;
            var padding = b64.EndsWith("==") ? 2 : (b64.EndsWith("=") ? 1 : 0);
            var bytes = (len * 3) / 4 - padding;

            return bytes > _maxBytes
                ? new ValidationResult(ErrorMessage)
                : ValidationResult.Success;
        }
    }
}

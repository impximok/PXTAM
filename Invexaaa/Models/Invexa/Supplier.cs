using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.Invexa
{
    public class Supplier
    {
        [Key]
        public int SupplierID { get; set; }

        [Required(ErrorMessage = "Supplier name is required.")]
        [MaxLength(100, ErrorMessage = "Supplier name must not exceed 100 characters.")]
        public string SupplierName { get; set; } = string.Empty;

        [MaxLength(100, ErrorMessage = "Contact person name must not exceed 100 characters.")]
        public string? SupplierContactPerson { get; set; }

        [Required(ErrorMessage = "Supplier phone is required.")]
        [MaxLength(20, ErrorMessage = "Supplier phone must not exceed 20 characters.")]
        public string SupplierPhone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string? SupplierEmail { get; set; }

        [MaxLength(255, ErrorMessage = "Supplier address must not exceed 255 characters.")]
        public string? SupplierAddress { get; set; }

        [Required(ErrorMessage = "Supplier lead time is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Lead time cannot be negative.")]
        public int SupplierLeadTimeDays { get; set; }

        [Required(ErrorMessage = "Supplier status is required.")]
        [MaxLength(20, ErrorMessage = "Supplier status must not exceed 20 characters.")]
        public string SupplierStatus { get; set; } = "Active";

        [MaxLength(255, ErrorMessage = "Profile image URL must not exceed 255 characters.")]
        public string? SupplierProfileImageUrl { get; set; }
    }
}

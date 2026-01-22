using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.Invexa
{
    public class Supplier
    {
        [Key]
        public int SupplierID { get; set; }

        // =========================
        // SUPPLIER NAME
        // =========================
        [Required(ErrorMessage = "Supplier name is required.")]
        [MaxLength(100, ErrorMessage = "Supplier name must not exceed 100 characters.")]
        [Display(Name = "Supplier Name")]
        public string SupplierName { get; set; } = string.Empty;

        // =========================
        // CONTACT PERSON
        // =========================
        [MaxLength(100, ErrorMessage = "Contact person name must not exceed 100 characters.")]
        [Display(Name = "Contact Person")]
        public string? SupplierContactPerson { get; set; }

        // =========================
        // PHONE (NUMBERS ONLY)
        // =========================
        [Required(ErrorMessage = "Supplier phone is required.")]
        [MaxLength(20, ErrorMessage = "Supplier phone must not exceed 20 characters.")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Phone number must contain numbers only.")]
        [Display(Name = "Phone Number")]
        public string SupplierPhone { get; set; } = string.Empty;

        // =========================
        // EMAIL
        // =========================
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string? SupplierEmail { get; set; }

        // =========================
        // ADDRESS
        // =========================
        [MaxLength(255, ErrorMessage = "Supplier address must not exceed 255 characters.")]
        [Display(Name = "Address")]
        public string? SupplierAddress { get; set; }

        // =========================
        // LEAD TIME (NON-NEGATIVE)
        // =========================
        [Required(ErrorMessage = "Supplier lead time is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Lead time must be 0 or greater.")]
        [Display(Name = "Lead Time (Days)")]
        public int SupplierLeadTimeDays { get; set; }

        // =========================
        // STATUS
        // =========================
        [Required(ErrorMessage = "Supplier status is required.")]
        [MaxLength(20, ErrorMessage = "Supplier status must not exceed 20 characters.")]
        [Display(Name = "Status")]
        public string SupplierStatus { get; set; } = "Active";

        // =========================
        // PROFILE IMAGE URL (OPTIONAL)
        // =========================
        [MaxLength(255, ErrorMessage = "Profile image URL must not exceed 255 characters.")]
        [Display(Name = "Profile Image URL")]
        public string? SupplierProfileImageUrl { get; set; }
    }
}

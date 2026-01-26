using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.Invexa
{
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        [MaxLength(100, ErrorMessage = "Category name must not exceed 100 characters.")]
        public string CategoryName { get; set; } = string.Empty;

        [MaxLength(255, ErrorMessage = "Category description must not exceed 255 characters.")]
        public string? CategoryDescription { get; set; }

        [Required(ErrorMessage = "Category status is required.")]
        [MaxLength(20, ErrorMessage = "Category status must not exceed 20 characters.")]
        public string? CategoryStatus { get; set; }

    }
}

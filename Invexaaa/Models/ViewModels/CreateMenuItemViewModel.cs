using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SnomiAssignmentReal.Models.ViewModels
{
    public class CreateMenuItemViewModel
    {
        [Required(ErrorMessage = "CategoryName is required.")]
        [MaxLength(100, ErrorMessage = "CategoryName must not exceed 100 characters.")]
        public string Name { get; set; }

        [MaxLength(255, ErrorMessage = "CategoryDescription must not exceed 255 characters.")]
        public string Description { get; set; }

        [Range(0, 4000, ErrorMessage = "MenuItemCalories must be between 0 and 4000.")]
        public int Calories { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        public string CategoryId { get; set; }

        [Required(ErrorMessage = "MenuItemUnitPrice is required.")]
        [Range(0.0, 1000.0, ErrorMessage = "MenuItemUnitPrice must be between RM0.00 and RM1000.00.")]
        public decimal Price { get; set; }

        // ⛔ REMOVE the boolean Range attribute — it prevented false from being posted.
        // Optional default: true (you can uncheck to set false on the form).
        public bool IsAvailable { get; set; } = true;

        // Optional image upload (keep as optional unless you truly want to require it)
        public IFormFile ImageFile { get; set; }
    }
}

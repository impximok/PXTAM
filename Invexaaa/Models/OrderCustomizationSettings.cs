using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnomiAssignmentReal.Models;

public class OrderCustomizationSettings
{
    [Key]
    public string MenuItemCustomizationId { get; set; } // e.g., "O100", auto-assigned elsewhere

    [Required(ErrorMessage = "Customization name is required.")]
    [MaxLength(100, ErrorMessage = "Customization name cannot exceed 100 characters.")]
    public string CustomizationName { get; set; }

    [MaxLength(255, ErrorMessage = "CategoryDescription cannot exceed 255 characters.")]
    public string CustomizationDescription { get; set; }

    [Range(0.0, 1000.0, ErrorMessage = "MenuItemUnitPrice must be between 0.00 and 1000.00.")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal CustomizationAdditionalPrice { get; set; }

    [MaxLength(50, ErrorMessage = "Eligible category ID cannot exceed 50 characters.")]
    public string EligibleCategoryId { get; set; }

    [Required(ErrorMessage = "Menu item ID is required.")]
    public string MenuItemId { get; set; }

    // Navigation property
    public MenuItem MenuItem { get; set; }

}

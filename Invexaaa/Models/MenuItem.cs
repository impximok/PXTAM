using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnomiAssignmentReal.Models;

public class MenuItem
{
    [Key]
    public string MenuItemId { get; set; } // e.g., "M100", auto-assigned elsewhere

    [Required(ErrorMessage = "Menu item name is required.")]
    [MaxLength(100, ErrorMessage = "Menu item name cannot exceed 100 characters.")]
    public string MenuItemName { get; set; }

    [MaxLength(255, ErrorMessage = "CategoryDescription cannot exceed 255 characters.")]
    public string MenuItemDescription { get; set; }

    [Range(0, 4000, ErrorMessage = "MenuItemCalories must be between 0 and 2000.")]
    public int MenuItemCalories { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    public string CategoryId { get; set; }

    // Navigation property for foreign key
    public Category Category { get; set; }

    [Required(ErrorMessage = "MenuItemUnitPrice is required.")]
    [Range(0.0, 1000.0, ErrorMessage = "MenuItemUnitPrice must be between 0 and 1000.")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal MenuItemUnitPrice { get; set; }

    public string MenuItemImageUrl { get; set; } // store path, not IFormFile

    public bool IsAvailableForOrder { get; set; } = true;

    // Navigation properties
    public ICollection<OrderDetail> MenuItemOrderDetails { get; set; } = new List<OrderDetail>();
    public ICollection<OrderCustomizationSettings> MenuItemCustomizations { get; set; } = new List<OrderCustomizationSettings>();
}

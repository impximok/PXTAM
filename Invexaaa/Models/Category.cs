using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnomiAssignmentReal.Models;

public class Category
{
    [Key]
    public string CategoryId { get; set; } // e.g., "CT100"

    [Required(ErrorMessage = "CategoryName is required.")]
    [MaxLength(50, ErrorMessage = "CategoryName must not exceed 50 characters.")]
    public string CategoryName { get; set; }

    [MaxLength(255, ErrorMessage = "CategoryDescription must not exceed 255 characters.")]
    public string? CategoryDescription { get; set; } // Optional

    // Navigation property: one category can have many menu items
    public ICollection<MenuItem>? CategoryMenuItems { get; set; }
}


using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.ViewModels
{
    public class AdjustStockViewModel
    {
        public List<int> InventoryIds { get; set; } = new();

        public int CurrentQuantity { get; set; }

        [Required]
        public int AdjustBy { get; set; }

        [Required(ErrorMessage = "Adjustment note is required.")]
        [MaxLength(255)]
        public string AdjustmentNote { get; set; } = string.Empty;

        // display-only
        public string? ItemName { get; set; }
        public string? ItemUnitOfMeasure { get; set; }
        public int ItemReorderLevel { get; set; }
        public int SafetyStock { get; set; }
        public int ReorderPoint { get; set; }
    }
}

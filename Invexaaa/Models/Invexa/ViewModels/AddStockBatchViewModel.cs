using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.ViewModels
{
    public class AddStockBatchViewModel
    {
        /* ===============================
           SHARED (ADD + ADJUST)
        =============================== */

        public List<int> InventoryIds { get; set; } = new();

        [Display(Name = "Quantity")]
        public int Quantity { get; set; }

        [Display(Name = "Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        /* ===============================
           ADD STOCK (BATCH)
        =============================== */

        [Display(Name = "Batch Number")]
        public string? BatchNumber { get; set; } // system-generated (not user typed)

        /* ===============================
           ADJUST STOCK
        =============================== */

        public int CurrentQuantity { get; set; }

        [Display(Name = "Adjust By")]
        public int AdjustBy { get; set; }

        [Required(ErrorMessage = "Adjustment note is required.")]
        [MaxLength(255)]
        public string? AdjustmentNote { get; set; }

        /* ===============================
           ITEM INFO (DISPLAY ONLY)
        =============================== */

        public string? ItemName { get; set; }
        public string? ItemUnitOfMeasure { get; set; }

        public int ItemReorderLevel { get; set; }
        public int SafetyStock { get; set; }
        public int ReorderPoint { get; set; }
    }
}

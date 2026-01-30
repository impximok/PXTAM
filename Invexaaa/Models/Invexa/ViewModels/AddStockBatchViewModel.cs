using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.ViewModels
{
    public class AddStockBatchViewModel : IValidatableObject
    {
        // =========================
        // INPUTS
        // =========================
        [MinLength(1, ErrorMessage = "No inventory items selected.")]
        public List<int> InventoryIds { get; set; } = new();

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }

        // =========================
        // COSTING & SUPPLIER (STOCK IN)
        // =========================
        [Required(ErrorMessage = "Unit cost is required")]
        [Range(0.01, double.MaxValue)]
        public decimal UnitCost { get; set; }

        public int? SupplierID { get; set; }

        [MaxLength(100)]
        public string? SupplierNameSnapshot { get; set; }

        [Range(0, 365)]
        public int LeadTimeDays { get; set; }


        [Required(ErrorMessage = "Expiry date is required")]
        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        [Required]
        public int InputQuantity { get; set; }

        [Required]
        public int SelectedUnitId { get; set; }

        // calculated (not user input)
        public int BaseQuantity { get; set; }
        public int UnitConversionID { get; set; }

        public List<ItemUnitConversion>? AvailableUnits { get; set; }




        // =========================
        // LIVE PREVIEW (BEFORE SAVE)
        // =========================
        public List<AddStockPreviewItem> PreviewItems { get; set; } = new();

        // =========================
        // RESULT SUMMARY (AFTER SAVE)
        // =========================
        public bool ShowSummary { get; set; } = false;

        public List<AddStockBatchSummaryRow> SummaryRows { get; set; } = new();

        // =========================
        // EXTRA VALIDATION
        // =========================
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Expiry must be in the future (date-only)
            if (ExpiryDate.HasValue && ExpiryDate.Value.Date <= DateTime.Today)
            {
                yield return new ValidationResult(
                    "Expiry date must be a future date.",
                    new[] { nameof(ExpiryDate) }
                );
            }
        }
    }

    // 🔵 Live preview row
    public class AddStockPreviewItem
    {
        public int InventoryID { get; set; }
        public string ItemName { get; set; } = "";
    }

    // 🟢 After-save summary row
    public class AddStockBatchSummaryRow
    {
        public string ItemName { get; set; } = "";
        public int QuantityAdded { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}

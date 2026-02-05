using Invexaaa.Models.Invexa.Enums;
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



        // =========================
        // COSTING & SUPPLIER (STOCK IN)
        // =========================
        
        public decimal? UnitCost { get; set; }

        public int? SupplierID { get; set; }

        [MaxLength(100)]
        public string? SupplierNameSnapshot { get; set; }

        public List<Supplier> Suppliers { get; set; } = new();

        [Range(0, 365)]
        public int LeadTimeDays { get; set; }


        [Required(ErrorMessage = "Expiry date is required")]
        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        [Required]
        public int InputQuantity { get; set; }

        

        // calculated (not user input)
        public int BaseQuantity { get; set; }
        public int? UnitConversionID { get; set; }

        // UI-only guard (no DB)
        public bool HasBaseUnit { get; set; }

        // Can user submit stock-in?
        public bool CanSubmit =>
            !(CostingMethod == CostingMethod.WeightedAverage ||
              CostingMethod == CostingMethod.Fixed)
            || HasBaseUnit;

        // Unit cost is locked for Fixed AND Weighted Average
        public bool IsUnitCostLocked =>
            CostingMethod == CostingMethod.Fixed ||
            CostingMethod == CostingMethod.WeightedAverage;

        public List<ItemUnitConversion>? AvailableUnits { get; set; }


        public CostingMethod CostingMethod { get; set; }

        // ✅ UI helper (NO DB column, NO migration)
        public bool IsFixedCosting => CostingMethod == CostingMethod.Fixed;

        public bool IsWeightedAverage => CostingMethod == CostingMethod.WeightedAverage;

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
            // Expiry must be in the future
            if (ExpiryDate.HasValue && ExpiryDate.Value.Date <= DateTime.Today)
            {
                yield return new ValidationResult(
                    "Expiry date must be a future date.",
                    new[] { nameof(ExpiryDate) }
                );
            }

            // FIFO requires unit cost
            if (CostingMethod == CostingMethod.FIFO && UnitCost == null)
            {
                yield return new ValidationResult(
                    "Unit cost is required for FIFO costing.",
                    new[] { nameof(UnitCost) }
                );
            }

            // Fixed / WeightedAverage must NOT accept manual unit cost
            if ((CostingMethod == CostingMethod.Fixed ||
                 CostingMethod == CostingMethod.WeightedAverage) &&
                UnitCost != null)
            {
                yield return new ValidationResult(
                    "Unit cost is system-controlled for this costing method.",
                    new[] { nameof(UnitCost) }
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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Invexaaa.Models.Invexa;

namespace Invexaaa.Models.ViewModels
{
    public class AdjustStockByBatchViewModel
    {
        public int InventoryID { get; set; }
        public int ItemID { get; set; }

        public string ItemName { get; set; } = "";
        public string ItemUnitOfMeasure { get; set; } = "";

        public int CurrentInventoryQuantity { get; set; }

        // =========================
        // MULTI-UOM INPUT
        // =========================
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
        public int InputQuantity { get; set; }

        [Required(ErrorMessage = "Please select a unit.")]
        public int UnitConversionID { get; set; }

        public int BaseQuantity { get; set; }

        // =========================
        // CUSTOMER (OPTIONAL)
        // =========================
        public int? CustomerID { get; set; }
        public string? CustomerNameSnapshot { get; set; }

        public List<Customer> Customers { get; set; } = new();

        // =========================
        // ADJUSTMENT
        // =========================
        [Required(ErrorMessage = "Adjustment reason is required.")]
        [StringLength(255)]
        public string AdjustmentReason { get; set; } = "";

        // =========================
        // UI DATA
        // =========================
        public List<ItemUnitConversion> AvailableUnits { get; set; } = new();
        public List<AdjustStockBatchRowViewModel> Batches { get; set; } = new();


    }
}

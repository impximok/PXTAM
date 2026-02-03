using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Invexaaa.Models.Invexa;

namespace Invexaaa.Models.ViewModels
{
    public class BulkMinusStockViewModel
    {
        public List<int> InventoryIds { get; set; } = new();

        // =========================
        // MULTI-UOM INPUT
        // =========================

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
        public int InputQuantity { get; set; }

        [Required(ErrorMessage = "Please select a unit.")]
        public int UnitConversionID { get; set; }

        // calculated (not user input)
        public int BaseQuantity { get; set; }

        // =========================
        // REASON
        // =========================
        [Required]
        public string Reason { get; set; } = "";

        // =========================
        // CUSTOMER (STOCK OUT)
        // =========================
        public int? CustomerID { get; set; }

        [MaxLength(100)]
        public string? CustomerNameSnapshot { get; set; }

        // =========================
        // UI DATA
        // =========================
        public List<ItemUnitConversion> AvailableUnits { get; set; } = new();
        public List<BulkMinusPreviewRow> PreviewItems { get; set; } = new();
        public List<Customer> Customers { get; set; } = new();

        public bool ShowSummary { get; set; } = false;
    }

    public class BulkMinusPreviewRow
    {
        public int InventoryID { get; set; }
        public string ItemName { get; set; } = "";
        public int AvailableQuantity { get; set; }
    }
}

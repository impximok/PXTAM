using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Invexaaa.Models.Invexa;
using Invexaaa.Models.Invexa.Enums;

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
        // STOCK OUT METADATA (AUDIT)
        // =========================
        [Required(ErrorMessage = "Stock out remark is required.")]
        [StringLength(255)]
        public string StockOutRemark { get; set; } = "";


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
        public List<FifoConsumptionRow> FifoConsumptions { get; set; } = new();

        public CostingMethod CostingMethod { get; set; }
    }

    public class BulkMinusPreviewRow
    {
        public int InventoryID { get; set; }
        public string ItemName { get; set; } = "";
        public int AvailableQuantity { get; set; }
    }

    public class FifoConsumptionRow
    {
        public string BatchNumber { get; set; } = "";
        public DateTime ExpiryDate { get; set; }
        public int QuantityConsumed { get; set; }
    }

}

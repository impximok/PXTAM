using Invexaaa.Models.Invexa.Enums;
using System;
using System.Collections.Generic;

namespace Invexaaa.Models.ViewModels
{
    public class ItemDetailViewModel
    {
        // Item
        public int ItemID { get; set; }
        public string ItemName { get; set; }
        public string CategoryName { get; set; }


        public string UnitOfMeasure { get; set; }
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }

        public int ReorderLevel { get; set; }
        public int SafetyStock { get; set; }

        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? ImageUrl { get; set; }
        public string? ItemBarcode { get; set; }

        public CostingMethod CostingMethod { get; set; }

        // Costing (read-only, derived from inventory)
        public decimal AverageUnitCost { get; set; }     // For Weighted Average
        public decimal StandardUnitCost { get; set; }    // For Fixed Costing
        public decimal TotalStockValue { get; set; }     // Qty × unit cost

       


        public int CurrentBalance { get; set; }
        public List<ItemBatchViewModel> Batches { get; set; }

        public List<ItemUnitConversion> Units { get; set; } = new();

    }
}

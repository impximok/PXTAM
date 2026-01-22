using System;

namespace Invexaaa.Models.ViewModels
{
    public class StockViewModel
    {
        public int InventoryID { get; set; }

        public int ItemID { get; set; }
        public string ItemName { get; set; } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;

        public int AvailableQuantity { get; set; }

        public string StockStatus { get; set; } = "In Stock";

        public DateTime LastUpdated { get; set; }

        // NEW (display only)
        public string? ItemImageUrl { get; set; }
    }
}

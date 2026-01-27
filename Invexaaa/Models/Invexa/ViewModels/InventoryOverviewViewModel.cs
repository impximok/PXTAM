using System;

namespace Invexaaa.Models.ViewModels
{
    public class InventoryOverviewViewModel
    {
        public int InventoryID { get; set; }
        public int ItemID { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public int TotalQuantity { get; set; }

        public string HealthStatus { get; set; } = "Healthy";

        // 🔑 REQUIRED for UI lock
        public string ItemStatus { get; set; } = "Active";

        public DateTime LastUpdated { get; set; }
    }
}

namespace Invexaaa.Models.ViewModels
{
    public class InventoryOverviewViewModel
    {
        public int InventoryID { get; set; }
        public int ItemID { get; set; }
        public string ItemName { get; set; } = string.Empty;

        public int TotalQuantity { get; set; }

        public string HealthStatus { get; set; } = "Healthy";

        public DateTime LastUpdated { get; set; }
    }
}

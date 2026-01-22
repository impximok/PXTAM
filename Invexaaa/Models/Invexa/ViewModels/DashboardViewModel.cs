namespace Invexaaa.Models.ViewModels
{
    public class DashboardViewModel
    {
        // Summary cards
        public int TotalItems { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public int ReorderAlertCount { get; set; }

        // Table data
        public List<InventoryRow> RecentInventories { get; set; } = new();
    }

    public class InventoryRow
    {
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}

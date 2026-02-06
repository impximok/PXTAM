using System.Collections.Generic;

namespace Invexaaa.Models.ViewModels
{
    public class DashboardViewModel
    {
        // ===============================
        // Summary cards
        // ===============================
        public int TotalItems { get; set; }
        public int ActiveItemCount { get; set; }
        public int InactiveItemCount { get; set; }

        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public int ReorderAlertCount { get; set; }

        // Stock overview
        public int OkStockCount { get; set; }

        // Table data
        public List<InventoryRow> RecentInventories { get; set; } = new();

        // ===============================
        // Sales Snapshot
        // ===============================
        public decimal TodaySales { get; set; }
        public int TodayOrders { get; set; }
        public decimal MonthSales { get; set; }
        public decimal AvgOrderValueToday { get; set; }

        // ===============================
        // Top Selling
        // ===============================
        public string TopSellingRange { get; set; } = "7d"; // default
        public List<TopSellingItemVm> TopSellingItems { get; set; } = new();

        // ===============================
        // Reorder Planner
        // ===============================
        public decimal TotalReorderCostEstimate { get; set; }
        public List<ReorderPlannerItemVm> ReorderPlanner { get; set; } = new();
    }

    public class InventoryRow
    {
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ItemStatus { get; set; } = "Active";
    }

    public class TopSellingItemVm
    {
        public int ItemID { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int QtySold { get; set; }
        public decimal SalesAmount { get; set; }
    }



    public class ReorderPlannerItemVm
    {
        public string ItemName { get; set; } = "";

        public int CurrentQty { get; set; }

        public int ReorderPoint { get; set; }
        public int SafetyStock { get; set; }

        public decimal AverageDailyDemand { get; set; }

        public int TargetStock { get; set; }
        public int SuggestedOrderQty { get; set; }

        public decimal? RunoutDays { get; set; }
    }

}

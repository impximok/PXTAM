using System;               // ⬅️ make sure this is there
using System.Collections.Generic;

namespace SnomiAssignmentReal.Models.ViewModels
{
    public class ReportVm
    {
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalItemsSold { get; set; }

        public List<BestSellerVm> BestSellingItems { get; set; } = new();
        public List<BestCustomizationVm> BestSellingCustomizations { get; set; } = new();

        // 🔹 New: report period
        public DateTime? PeriodStart { get; set; }
        public DateTime? PeriodEnd { get; set; }
    }

    public class BestSellerVm
    {
        public string ItemName { get; set; } = "";
        public int QuantitySold { get; set; }
    }

    public class BestCustomizationVm
    {
        public string CustomizationName { get; set; } = "";
        public int TimesChosen { get; set; }
        public decimal TotalAddOnRevenue { get; set; }
    }
}

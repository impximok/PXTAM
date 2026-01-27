using Invexaaa.Models.Invexa;
using Invexaaa.Models.Invexa.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Invexaaa.Models.ViewModels
{
    public class ReportPrintViewModel
    {
        public string ReportType { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public List<SalesHeader>? SalesHeaders { get; set; }
        public List<DemandForecastReportViewModel>? DemandForecastReport { get; set; }
        public List<StockSummaryViewModel>? StockSummary { get; set; }
        public List<ExpiryReportViewModel>? ExpiryReport { get; set; }
        public List<StockChartViewModel>? StockChartData { get; set; }

        public DateTime GeneratedOn { get; set; }
        public string ScopeNote { get; set; } = "";
        public string? ChartNote { get; set; }

        // KPIs
        public int TotalQuantityAll => StockSummary?.Sum(x => x.Quantity) ?? 0;
        public int TotalQuantityActive => StockSummary?.Where(x => x.ItemStatus == "Active").Sum(x => x.Quantity) ?? 0;
        public int InactiveItemCount => StockSummary?.Count(x => x.ItemStatus == "Inactive") ?? 0;

        public decimal InventoryValueActive =>
            StockSummary?.Where(x => x.ItemStatus == "Active")
            .Sum(x => x.Quantity * x.BuyPrice) ?? 0;
    }
}

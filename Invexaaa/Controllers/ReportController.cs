using Invexaaa.Data;
using Invexaaa.Models.Invexa.ViewModels;
using Invexaaa.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Rotativa.AspNetCore;
using System;
using System.Linq;

namespace Invexaaa.Controllers
{
    public class ReportsController : Controller
    {
        private readonly InvexaDbContext _context;

        public ReportsController(InvexaDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // OVERVIEW PAGE (GET)
        // =====================================================
        [HttpGet]
        public IActionResult Overview()
        {
            return View(new ReportPrintViewModel());
        }

        // =====================================================
        // GENERATE REPORT (POST → SAME PAGE)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Overview(string reportType, DateTime? startDate, DateTime? endDate)
        {
            var vm = BuildReportViewModel(reportType, startDate, endDate);
            if (reportType == "StockCharts")
            {
                return View("StockCharts", vm);
            }

            return View(vm);
            // 👈 stays on Overview
        }

        // =====================================================
        // EXPORT PDF (REAL PDF)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExportPdf(string reportType, DateTime? startDate, DateTime? endDate)
        {
            var vm = BuildReportViewModel(reportType, startDate, endDate);

            return new ViewAsPdf("Print", vm)
            {
                FileName = $"{reportType}_Report_{DateTime.Now:yyyyMMdd}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4
            };
        }

        // =====================================================
        // SHARED REPORT BUILDER
        // =====================================================
        private ReportPrintViewModel BuildReportViewModel(
    string reportType,
    DateTime? startDate,
    DateTime? endDate)
        {
            var vm = new ReportPrintViewModel
            {
                ReportType = reportType,
                StartDate = startDate,
                EndDate = endDate,
                GeneratedOn = DateTime.Now,
                ScopeNote = "Reports include BOTH Active and Inactive items unless explicitly stated."
            };

            if (string.IsNullOrEmpty(reportType))
                return vm;

            switch (reportType)
            {
                // ================= SALES =================
                case "Sales":
                    vm.SalesHeaders = _context.SalesHeaders
                        .Where(x =>
                            (!startDate.HasValue || x.SalesDate >= startDate) &&
                            (!endDate.HasValue || x.SalesDate <= endDate))
                        .OrderByDescending(x => x.SalesDate)
                        .ToList();
                    break;

                // ================= DEMAND FORECAST (OPERATIONAL) =================
                case "DemandForecast":

                    var forecastStart = startDate ?? DateTime.MinValue;
                    var forecastEnd = endDate ?? DateTime.Today;

                    var totalDays = (forecastEnd - forecastStart).Days;
                    if (totalDays <= 0) totalDays = 1;

                    var usageData =
                        from t in _context.StockTransactions
                        where t.TransactionType == "OUT"
                              && (!startDate.HasValue || t.TransactionDate >= forecastStart)
                              && (!endDate.HasValue || t.TransactionDate <= forecastEnd)
                        group t by t.ItemID into g
                        select new
                        {
                            ItemID = g.Key,
                            TotalUsed = g.Sum(x => x.TransactionQuantity)
                        };

                    vm.DemandForecastReport =
                        (from u in usageData
                         join i in _context.Items on u.ItemID equals i.ItemID
                         let avgDaily = (decimal)u.TotalUsed / totalDays
                         select new DemandForecastReportViewModel
                         {
                             ItemID = i.ItemID,
                             ItemName = i.ItemName,
                             ItemStatus = i.ItemStatus,

                             Period = startDate.HasValue || endDate.HasValue
                                 ? $"{forecastStart:yyyy-MM-dd} → {forecastEnd:yyyy-MM-dd}"
                                 : "All historical data",

                             // ✅ NEW
                             AverageDailyDemand = Math.Round(avgDaily, 2),

                             ForecastQty = (int)Math.Ceiling(avgDaily * totalDays),

                             RecommendedReorder =
                                 (int)Math.Ceiling((avgDaily * totalDays) + i.SafetyStock)
                         })
                        .OrderByDescending(x => x.AverageDailyDemand)
                        .ToList();

                    break;



                // ================= STOCK SUMMARY (KPIs COME FROM HERE) =================
                case "Stock":
                    vm.StockSummary =
                        (from inv in _context.Inventories
                         join i in _context.Items on inv.ItemID equals i.ItemID
                         select new StockSummaryViewModel
                         {
                             ItemID = i.ItemID,
                             ItemName = i.ItemName,
                             ItemStatus = i.ItemStatus,
                             Quantity = inv.InventoryTotalQuantity,
                             BuyPrice = i.ItemBuyPrice,
                             LastUpdated = inv.InventoryLastUpdated
                         }).ToList();
                    break;

                // ================= STOCK CHARTS (ACTIVE ONLY) =================
                case "StockCharts":
                    vm.StockChartData =
                        (from inv in _context.Inventories
                         join i in _context.Items on inv.ItemID equals i.ItemID
                         where i.ItemStatus == "Active" // 👈 EXPLICIT RULE
                         select new StockChartViewModel
                         {
                             ItemName = i.ItemName,
                             Quantity = inv.InventoryTotalQuantity,
                             Status =
                                inv.InventoryTotalQuantity <= i.ReorderPoint ? "Reorder" :
                                inv.InventoryTotalQuantity <= i.ItemReorderLevel ? "Low" :
                                "Healthy"
                         }).ToList();

                    vm.ChartNote =
                        "Charts include ACTIVE items only. Inactive items are excluded to avoid analytical distortion.";
                    break;

                // ================= EXPIRY & WASTAGE =================
                case "Expiry":
                    vm.ExpiryReport =
                        (from b in _context.StockBatches
                         join i in _context.Items on b.ItemID equals i.ItemID
                         where b.BatchExpiryDate <= DateTime.Today.AddDays(30)
                         orderby b.BatchExpiryDate
                         select new ExpiryReportViewModel
                         {
                             BatchNumber = b.BatchNumber,
                             ItemName = i.ItemName,
                             ItemStatus = i.ItemStatus,
                             Quantity = b.BatchQuantity,
                             ExpiryDate = b.BatchExpiryDate
                         }).ToList();
                    break;
            }

            return vm;
        }




    }
}

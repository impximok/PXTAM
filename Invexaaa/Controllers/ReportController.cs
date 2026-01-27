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

                // ================= DEMAND FORECAST =================
                case "DemandForecast":
                    vm.DemandForecastReport =
                        _context.DemandForecasts
                        .AsEnumerable() // 🔑 SWITCH TO IN-MEMORY LINQ
                        .Join(
                            _context.Items,
                            d => d.ItemID,
                            i => i.ItemID,
                            (d, i) => new
                            {
                                Forecast = d,
                                Item = i,
                                ParsedPeriod = DateTime.TryParse(d.DemandForecastPeriod, out var dt)
                                    ? (DateTime?)dt
                                    : null
                            })
                        .Where(x =>
                            (!startDate.HasValue || (x.ParsedPeriod.HasValue && x.ParsedPeriod >= startDate)) &&
                            (!endDate.HasValue || (x.ParsedPeriod.HasValue && x.ParsedPeriod <= endDate)))
                        .OrderByDescending(x => x.ParsedPeriod)
                        .Select(x => new DemandForecastReportViewModel
                        {
                            ItemID = x.Item.ItemID,
                            ItemName = x.Item.ItemName,
                            ItemStatus = x.Item.ItemStatus,
                            Period = x.Forecast.DemandForecastPeriod,
                            ForecastQty = x.Forecast.DemandPredictedQuantity,
                            RecommendedReorder = x.Forecast.DemandRecommendedReorderQty
                        })
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

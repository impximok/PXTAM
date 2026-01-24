using Invexaaa.Data;
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
            return View(vm); // 👈 stays on Overview
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
                EndDate = endDate
            };

            switch (reportType)
            {
                case "Sales":
                    vm.SalesHeaders = _context.SalesHeaders
                        .Where(x =>
                            (!startDate.HasValue || x.SalesDate >= startDate) &&
                            (!endDate.HasValue || x.SalesDate <= endDate))
                        .OrderByDescending(x => x.SalesDate)
                        .ToList();
                    break;

                case "DemandForecast":
                    vm.DemandForecasts = _context.DemandForecasts
                        .OrderByDescending(x => x.DemandGeneratedDate)
                        .ToList();
                    break;

                case "Stock":
                    vm.Inventories = _context.Inventories.ToList();
                    break;

                case "Expiry":
                    vm.StockBatches = _context.StockBatches
                        .Where(x => x.BatchExpiryDate <= DateTime.Now.AddDays(30))
                        .OrderBy(x => x.BatchExpiryDate)
                        .ToList();
                    break;
            }

            return vm;
        }
    }
}

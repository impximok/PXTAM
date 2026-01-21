// Controllers/AdminController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnomiAssignmentReal.Data;
using SnomiAssignmentReal.Models;
using SnomiAssignmentReal.Models.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Globalization;
using Rotativa.AspNetCore;
using ClosedXML.Excel;
using System.IO;


namespace SnomiAssignmentReal.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    [Route("Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ---------------- Dashboard (role-based, no DB lookups) ----------------
        [HttpGet("Dashboard")]
        public IActionResult Dashboard()
        {
            // Works with either names ("Admin"/"Staff") or your seeded ids ("UR100"/"UR101")
            bool isAdmin = User.IsInRole("Admin") || User.IsInRole("UR100");
            bool isStaff = User.IsInRole("Staff") || User.IsInRole("UR101");

            // F100 Manage Menu
            ViewBag.CanManageMenu = isAdmin;
            // F101 View/Manage CustomerOrders
            ViewBag.CanViewOrders = isAdmin || isStaff;
            // F102 Order History
            ViewBag.CanOrderHistory = isAdmin || isStaff;
            // F103 View Report
            ViewBag.CanViewReport = isAdmin;
            // F104 Manage accounts
            ViewBag.CanManageAccounts = isAdmin;

            // Optional: tracking page for both
            ViewBag.CanTrackOrders = isAdmin || isStaff;

            return View(); // Views/Admin/Dashboard.cshtml
        }

        // ---------------- Report (Admin only) ----------------
        [HttpGet("Report")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Report(DateTime? startDate, DateTime? endDate)
        {
            // Start with base query
            var q = _db.CustomerOrders
                .Include(o => o.CustomerOrderDetails).ThenInclude(od => od.MenuItem)
                .Include(o => o.CustomerOrderDetails).ThenInclude(od => od.AppliedCustomizations)
                .AsQueryable();

            // Apply date filters if provided
            if (startDate.HasValue)
            {
                var s = startDate.Value.Date;
                q = q.Where(o => o.OrderCreatedAt >= s);
            }

            if (endDate.HasValue)
            {
                var e = endDate.Value.Date.AddDays(1); // inclusive
                q = q.Where(o => o.OrderCreatedAt < e);
            }

            var orders = await q.ToListAsync();

            var totalOrders = orders.Count;

            var totalRevenue = orders.Sum(o =>
                o.CustomerOrderDetails.Sum(od =>
                    (((od.MenuItem?.MenuItemUnitPrice) ?? 0m) +
                     ((od.AppliedCustomizations?.Sum(c => c.CustomizationAdditionalPrice)) ?? 0m))
                    * od.OrderedQuantity));

            var totalItemsSold = orders.Sum(o => o.CustomerOrderDetails.Sum(od => od.OrderedQuantity));

            var bestSellers = orders
                .SelectMany(o => o.CustomerOrderDetails)
                .GroupBy(od => od.MenuItem?.MenuItemName ?? "Unknown Item")
                .Select(g => new BestSellerVm
                {
                    ItemName = g.Key,
                    QuantitySold = g.Sum(x => x.OrderedQuantity)
                })
                .OrderByDescending(x => x.QuantitySold)
                .Take(5)
                .ToList();

            var bestCustomizations = orders
                .SelectMany(o => o.CustomerOrderDetails)
                .SelectMany(od => od.AppliedCustomizations ?? Enumerable.Empty<OrderCustomizationSettings>())
                .GroupBy(c => c.CustomizationName)
                .Select(g => new BestCustomizationVm
                {
                    CustomizationName = g.Key,
                    TimesChosen = g.Count(),
                    TotalAddOnRevenue = g.Sum(x => x.CustomizationAdditionalPrice)
                })
                .OrderByDescending(x => x.TimesChosen)
                .Take(5)
                .ToList();

            // Decide what to show as "report period"
            DateTime? periodStart = null;
            DateTime? periodEnd = null;

            if (startDate.HasValue || endDate.HasValue)
            {
                periodStart = startDate;
                periodEnd = endDate;
            }
            else if (orders.Any())
            {
                periodStart = orders.Min(o => (DateTime?)o.OrderCreatedAt);
                periodEnd = orders.Max(o => (DateTime?)o.OrderCreatedAt);
            }

            var vm = new ReportVm
            {
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                TotalItemsSold = totalItemsSold,
                BestSellingItems = bestSellers,
                BestSellingCustomizations = bestCustomizations,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd
            };

            // Keep the inputs pre-filled with selected values
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(vm);
        }

        // ---------------- Download Report (CSV) ----------------
        [HttpGet("DownloadReport")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DownloadReport(DateTime? startDate, DateTime? endDate)
        {
            var q = _db.CustomerOrders
                .Include(o => o.CustomerOrderDetails).ThenInclude(od => od.MenuItem)
                .Include(o => o.CustomerOrderDetails).ThenInclude(od => od.AppliedCustomizations)
                .AsQueryable();

            if (startDate.HasValue)
            {
                var s = startDate.Value.Date;
                q = q.Where(o => o.OrderCreatedAt >= s);
            }

            if (endDate.HasValue)
            {
                var e = endDate.Value.Date.AddDays(1); // inclusive
                q = q.Where(o => o.OrderCreatedAt < e);
            }

            var orders = await q.ToListAsync();

            var totalOrders = orders.Count;

            var totalRevenue = orders.Sum(o =>
                o.CustomerOrderDetails.Sum(od =>
                    (((od.MenuItem?.MenuItemUnitPrice) ?? 0m) +
                     ((od.AppliedCustomizations?.Sum(c => c.CustomizationAdditionalPrice)) ?? 0m))
                    * od.OrderedQuantity));

            var totalItemsSold = orders.Sum(o => o.CustomerOrderDetails.Sum(od => od.OrderedQuantity));

            var bestSellers = orders
                .SelectMany(o => o.CustomerOrderDetails)
                .GroupBy(od => od.MenuItem?.MenuItemName ?? "Unknown Item")
                .Select(g => new BestSellerVm
                {
                    ItemName = g.Key,
                    QuantitySold = g.Sum(x => x.OrderedQuantity)
                })
                .OrderByDescending(x => x.QuantitySold)
                .Take(5)
                .ToList();

            var bestCustomizations = orders
                .SelectMany(o => o.CustomerOrderDetails)
                .SelectMany(od => od.AppliedCustomizations ?? Enumerable.Empty<OrderCustomizationSettings>())
                .GroupBy(c => c.CustomizationName)
                .Select(g => new BestCustomizationVm
                {
                    CustomizationName = g.Key,
                    TimesChosen = g.Count(),
                    TotalAddOnRevenue = g.Sum(x => x.CustomizationAdditionalPrice)
                })
                .OrderByDescending(x => x.TimesChosen)
                .Take(5)
                .ToList();

            DateTime? periodStart = null;
            DateTime? periodEnd = null;

            if (startDate.HasValue || endDate.HasValue)
            {
                periodStart = startDate;
                periodEnd = endDate;
            }
            else if (orders.Any())
            {
                periodStart = orders.Min(o => (DateTime?)o.OrderCreatedAt);
                periodEnd = orders.Max(o => (DateTime?)o.OrderCreatedAt);
            }

            string periodText =
                (periodStart?.ToString("dd/MM/yyyy") ?? "N/A") + " - " +
                (periodEnd?.ToString("dd/MM/yyyy") ?? "N/A");

            var sb = new StringBuilder();

            sb.AppendLine($"Cafe Report ({periodText})");
            sb.AppendLine();

            sb.AppendLine("Summary");
            sb.AppendLine($"Total Orders,{totalOrders}");
            sb.AppendLine($"Total Revenue,{totalRevenue:F2}");
            sb.AppendLine($"Total Items Sold,{totalItemsSold}");
            sb.AppendLine();

            sb.AppendLine("Best Selling Items");
            sb.AppendLine("Item,Quantity Sold");
            foreach (var item in bestSellers)
            {
                sb.AppendLine($"\"{item.ItemName}\",{item.QuantitySold}");
            }
            sb.AppendLine();

            sb.AppendLine("Best Customizations");
            sb.AppendLine("Customization,Times Chosen,Total Add-on Revenue");
            foreach (var c in bestCustomizations)
            {
                sb.AppendLine($"\"{c.CustomizationName}\",{c.TimesChosen},{c.TotalAddOnRevenue:F2}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"CafeReport_{(startDate?.ToString("yyyyMMdd") ?? "all")}_{(endDate?.ToString("yyyyMMdd") ?? "all")}.csv";

            return File(bytes, "text/csv", fileName);
        }

        // ---------------- Download Report as Excel (.xlsx) ----------------
        [HttpGet("DownloadReportExcel")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DownloadReportExcel(DateTime? startDate, DateTime? endDate)
        {
            // 1. Query + aggregate data (same logic as Report)
            var q = _db.CustomerOrders
                .Include(o => o.CustomerOrderDetails).ThenInclude(od => od.MenuItem)
                .Include(o => o.CustomerOrderDetails).ThenInclude(od => od.AppliedCustomizations)
                .AsQueryable();

            if (startDate.HasValue)
            {
                var s = startDate.Value.Date;
                q = q.Where(o => o.OrderCreatedAt >= s);
            }

            if (endDate.HasValue)
            {
                var e = endDate.Value.Date.AddDays(1); // inclusive
                q = q.Where(o => o.OrderCreatedAt < e);
            }

            var orders = await q.ToListAsync();

            var totalOrders = orders.Count;

            var totalRevenue = orders.Sum(o =>
                o.CustomerOrderDetails.Sum(od =>
                    (((od.MenuItem?.MenuItemUnitPrice) ?? 0m) +
                     ((od.AppliedCustomizations?.Sum(c => c.CustomizationAdditionalPrice)) ?? 0m))
                    * od.OrderedQuantity));

            var totalItemsSold = orders.Sum(o => o.CustomerOrderDetails.Sum(od => od.OrderedQuantity));

            var bestSellers = orders
                .SelectMany(o => o.CustomerOrderDetails)
                .GroupBy(od => od.MenuItem?.MenuItemName ?? "Unknown Item")
                .Select(g => new BestSellerVm
                {
                    ItemName = g.Key,
                    QuantitySold = g.Sum(x => x.OrderedQuantity)
                })
                .OrderByDescending(x => x.QuantitySold)
                .Take(5)
                .ToList();

            var bestCustomizations = orders
                .SelectMany(o => o.CustomerOrderDetails)
                .SelectMany(od => od.AppliedCustomizations ?? Enumerable.Empty<OrderCustomizationSettings>())
                .GroupBy(c => c.CustomizationName)
                .Select(g => new BestCustomizationVm
                {
                    CustomizationName = g.Key,
                    TimesChosen = g.Count(),
                    TotalAddOnRevenue = g.Sum(x => x.CustomizationAdditionalPrice)
                })
                .OrderByDescending(x => x.TimesChosen)
                .Take(5)
                .ToList();

            DateTime? periodStart = null;
            DateTime? periodEnd = null;

            if (startDate.HasValue || endDate.HasValue)
            {
                periodStart = startDate;
                periodEnd = endDate;
            }
            else if (orders.Any())
            {
                periodStart = orders.Min(o => (DateTime?)o.OrderCreatedAt);
                periodEnd = orders.Max(o => (DateTime?)o.OrderCreatedAt);
            }

            string periodText;
            if (periodStart.HasValue && periodEnd.HasValue)
            {
                periodText = $"{periodStart.Value:dd/MM/yyyy} – {periodEnd.Value:dd/MM/yyyy}";
            }
            else
            {
                periodText = "–";
            }

            var fileName = $"CafeReport_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

            // 2. Build Excel workbook with a polished layout
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Snomi Café Report");

            // ========= HEADER =========
            // Title
            ws.Range("A1:F1").Merge();
            ws.Cell("A1").Value = "SNOMI CAFE SALES REPORT";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 18;
            ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Period
            ws.Range("A2:F2").Merge();
            ws.Cell("A2").Value = $"Period: {periodText}";
            ws.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell("A2").Style.Font.FontColor = XLColor.DarkGray;

            // Accent line under header
            var headerAccent = ws.Range("A2:F2");
            headerAccent.Style.Border.BottomBorder = XLBorderStyleValues.Thick;
            headerAccent.Style.Border.BottomBorderColor = XLColor.ForestGreen;

            // Generated at
            ws.Cell("A4").Value = "Generated:";
            ws.Cell("A4").Style.Font.Bold = true;
            ws.Cell("B4").Value = DateTime.Now.ToString("dd/MM/yyyy hh:mm tt");

            // Optional small system note
            ws.Cell("A5").Value = "System:";
            ws.Cell("A5").Style.Font.Bold = true;
            ws.Cell("B5").Value = "Snomi Café Ordering System";

            // ========= KPI “CARDS” =========
            int kpiTop = 7;

            // Card 1: Total Orders (columns A–B)
            var k1Header = ws.Range(kpiTop, 1, kpiTop, 2);
            k1Header.Merge();
            k1Header.Value = "Total Orders";
            k1Header.Style.Font.Bold = true;
            k1Header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            k1Header.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");

            var k1Value = ws.Range(kpiTop + 1, 1, kpiTop + 1, 2);
            k1Value.Merge();
            k1Value.Value = totalOrders;
            k1Value.Style.Font.Bold = true;
            k1Value.Style.Font.FontSize = 14;
            k1Value.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Range(kpiTop, 1, kpiTop + 1, 2).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // Card 2: Total Revenue (columns C–D)
            var k2Header = ws.Range(kpiTop, 3, kpiTop, 4);
            k2Header.Merge();
            k2Header.Value = "Total Revenue (RM)";
            k2Header.Style.Font.Bold = true;
            k2Header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            k2Header.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");

            var k2Value = ws.Range(kpiTop + 1, 3, kpiTop + 1, 4);
            k2Value.Merge();
            k2Value.Value = totalRevenue;
            k2Value.Style.Font.Bold = true;
            k2Value.Style.Font.FontSize = 14;
            k2Value.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            k2Value.Style.NumberFormat.Format = "#,##0.00";

            ws.Range(kpiTop, 3, kpiTop + 1, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // Card 3: Total Items Sold (columns E–F)
            var k3Header = ws.Range(kpiTop, 5, kpiTop, 6);
            k3Header.Merge();
            k3Header.Value = "Total Items Sold";
            k3Header.Style.Font.Bold = true;
            k3Header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            k3Header.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");

            var k3Value = ws.Range(kpiTop + 1, 5, kpiTop + 1, 6);
            k3Value.Merge();
            k3Value.Value = totalItemsSold;
            k3Value.Style.Font.Bold = true;
            k3Value.Style.Font.FontSize = 14;
            k3Value.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Range(kpiTop, 5, kpiTop + 1, 6).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // ========= BEST SELLING ITEMS =========
            int row = kpiTop + 4;

            ws.Cell(row, 1).Value = "Best Selling Items";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 12;
            row += 1;

            // Table header
            ws.Cell(row, 1).Value = "Item";
            ws.Cell(row, 2).Value = "Quantity Sold";
            var itemsHeaderRange = ws.Range(row, 1, row, 2);
            itemsHeaderRange.Style.Font.Bold = true;
            itemsHeaderRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            row++;

            // Table rows
            foreach (var item in bestSellers)
            {
                ws.Cell(row, 1).Value = item.ItemName;
                ws.Cell(row, 2).Value = item.QuantitySold;
                row++;
            }

            if (bestSellers.Any())
            {
                var dataRange = ws.Range(row - bestSellers.Count, 1, row - 1, 2);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            // ========= BEST CUSTOMIZATIONS =========
            row += 2;

            ws.Cell(row, 1).Value = "Best Customizations";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 12;
            row++;

            ws.Cell(row, 1).Value = "Customization";
            ws.Cell(row, 2).Value = "Times Chosen";
            ws.Cell(row, 3).Value = "Add-on Revenue (RM)";
            var custHeaderRange = ws.Range(row, 1, row, 3);
            custHeaderRange.Style.Font.Bold = true;
            custHeaderRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            row++;

            foreach (var c in bestCustomizations)
            {
                ws.Cell(row, 1).Value = c.CustomizationName;
                ws.Cell(row, 2).Value = c.TimesChosen;
                ws.Cell(row, 3).Value = c.TotalAddOnRevenue;
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                row++;
            }

            if (bestCustomizations.Any())
            {
                var dataRange = ws.Range(row - bestCustomizations.Count, 1, row - 1, 3);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            // ========= FOOTER NOTE =========
            row += 2;
            ws.Cell(row, 1).Value = "Report generated by Snomi Café Ordering System.";
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.DarkGray;

            // Auto-fit columns
            ws.Columns().AdjustToContents();

            // 3. Return file
            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            var content = stream.ToArray();

            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }



        // ---------------- Manage Customers (Admin only) ----------------
        [HttpGet("ManageCustomers")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageCustomers(
            string? search,
            string userType = "all",
            string ordersFilter = "all",
            string sort = "recent_desc",
            int? minPoints = null,
            int? maxPoints = null,
            int page = 1,
            int pageSize = 10)
        {
            var q = _db.Customers.Include(c => c.CustomerOrders).AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(c =>
                    (c.CustomerFullName ?? "").ToLower().Contains(s) ||
                    (c.CustomerUserName ?? "").ToLower().Contains(s) ||
                    (c.CustomerEmailAddress ?? "").ToLower().Contains(s) ||
                    (c.CustomerPhoneNumber ?? "").ToLower().Contains(s) ||
                    c.CustomerId.ToLower().Contains(s));
            }

            switch ((userType ?? "all").ToLower())
            {
                case "registered": q = q.Where(c => c.CustomerUserName != null || c.CustomerPasswordHash != null); break;
                case "guests": q = q.Where(c => c.CustomerUserName == null && c.CustomerPasswordHash == null); break;
            }

            switch ((ordersFilter ?? "all").ToLower())
            {
                case "with": q = q.Where(c => c.CustomerOrders.Any()); break;
                case "without": q = q.Where(c => !c.CustomerOrders.Any()); break;
            }

            if (string.Equals(userType, "registered", StringComparison.OrdinalIgnoreCase))
            {
                if (minPoints.HasValue) q = q.Where(c => c.CustomerRewardPoints >= minPoints.Value);
                if (maxPoints.HasValue) q = q.Where(c => c.CustomerRewardPoints <= maxPoints.Value);
            }

            q = (sort ?? "recent_desc") switch
            {
                "name_asc" => q.OrderBy(c => c.CustomerFullName),
                "name_desc" => q.OrderByDescending(c => c.CustomerFullName),
                "points_asc" => q.OrderBy(c => c.CustomerRewardPoints).ThenByDescending(c => c.CustomerOrders.Max(o => (DateTime?)o.OrderCreatedAt)),
                "points_desc" => q.OrderByDescending(c => c.CustomerRewardPoints).ThenByDescending(c => c.CustomerOrders.Max(o => (DateTime?)o.OrderCreatedAt)),
                "orders_asc" => q.OrderBy(c => c.CustomerOrders.Count).ThenByDescending(c => c.CustomerOrders.Max(o => (DateTime?)o.OrderCreatedAt)),
                "orders_desc" => q.OrderByDescending(c => c.CustomerOrders.Count).ThenByDescending(c => c.CustomerOrders.Max(o => (DateTime?)o.OrderCreatedAt)),
                "id_asc" => q.OrderBy(c => c.CustomerId),
                "id_desc" => q.OrderByDescending(c => c.CustomerId),
                "recent_asc" => q.OrderBy(c => c.CustomerOrders.Max(o => (DateTime?)o.OrderCreatedAt)),
                _ => q.OrderByDescending(c => c.CustomerOrders.Max(o => (DateTime?)o.OrderCreatedAt)),
            };

            page = Math.Max(1, page);
            pageSize = Math.Max(1, pageSize);
            var total = await q.CountAsync();
            var customers = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var vm = new ManageCustomersVm
            {
                Customers = customers,
                Search = search,
                UserType = userType,
                OrdersFilter = ordersFilter,
                SortBy = sort,
                MinPoints = minPoints,
                MaxPoints = maxPoints,
                Page = page,
                PageSize = pageSize,
                TotalItems = total
            };

            return View(vm);
        }

        // ---------------- Actions: Points & Delete (Admin only) ----------------
        [HttpPost("AdjustPoints")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustPoints(string id, int delta)
        {
            var c = await _db.Customers.FirstOrDefaultAsync(x => x.CustomerId == id);
            if (c == null) { TempData["Error"] = "Customer not found."; return RedirectToAction(nameof(ManageCustomers)); }

            if (c.CustomerUserName == null && c.CustomerPasswordHash == null)
            { TempData["Error"] = "Guests do not have reward points."; return RedirectToAction(nameof(ManageCustomers)); }

            c.CustomerRewardPoints = Math.Max(0, c.CustomerRewardPoints + delta);
            await _db.SaveChangesAsync();
            TempData["Info"] = $"Points updated to {c.CustomerRewardPoints}.";
            return RedirectToAction(nameof(ManageCustomers));
        }

        [HttpPost("SetPoints")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPoints(string id, int points)
        {
            if (points < 0) points = 0;
            var c = await _db.Customers.FirstOrDefaultAsync(x => x.CustomerId == id);
            if (c == null) { TempData["Error"] = "Customer not found."; return RedirectToAction(nameof(ManageCustomers)); }
            if (c.CustomerUserName == null && c.CustomerPasswordHash == null)
            { TempData["Error"] = "Guests do not have reward points."; return RedirectToAction(nameof(ManageCustomers)); }

            c.CustomerRewardPoints = points;
            await _db.SaveChangesAsync();
            TempData["Info"] = $"Points set to {c.CustomerRewardPoints}.";
            return RedirectToAction(nameof(ManageCustomers));
        }

        [HttpPost("DeleteCustomer")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCustomer(string id)
        {
            var c = await _db.Customers.FirstOrDefaultAsync(x => x.CustomerId == id);
            if (c == null) { TempData["Error"] = "Customer not found."; return RedirectToAction(nameof(ManageCustomers)); }

            var hasOrders = await _db.CustomerOrders.AnyAsync(o => o.CustomerId == id);
            if (hasOrders) { TempData["Error"] = "Cannot delete a customer who has orders."; return RedirectToAction(nameof(ManageCustomers)); }

            _db.Customers.Remove(c);
            await _db.SaveChangesAsync();
            TempData["Info"] = "Customer deleted.";
            return RedirectToAction(nameof(ManageCustomers));
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Invexaaa.Data;
using Invexaaa.Models.Invexa;
using OfficeOpenXml;
using Microsoft.EntityFrameworkCore;
using System.Globalization;


namespace Invexaaa.Controllers
{
    public class SupplierController : Controller
    {
        private readonly InvexaDbContext _context;

        public SupplierController(InvexaDbContext context)
        {
            _context = context;
        }

        // =========================
        // LIST (Maintain Supplier)
        // =========================
        public IActionResult SupplierIndex()
        {
            var suppliers = _context.Suppliers.ToList();
            return View("SupplierIndex", suppliers);
        }

        // =========================
        // CREATE
        // =========================
        public IActionResult Create()
        {
            return View("CreateSupplier");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Supplier supplier, string submitAction)
        {
            if (!ModelState.IsValid)
            {
                // Validation failed → stay on same page with data
                return View("CreateSupplier", supplier);
            }

            _context.Suppliers.Add(supplier);
            _context.SaveChanges();

            // SAVE & NEW → stay on page and clear form
            if (submitAction == "saveNew")
            {
                ModelState.Clear();          // clears validation + old values
                return View("CreateSupplier", new Supplier());
            }

            // SAVE → go back to list
            return RedirectToAction(nameof(SupplierIndex));
        }


        // =========================
        // EDIT
        // =========================
        public IActionResult Edit(int id)
        {
            var supplier = _context.Suppliers.Find(id);
            if (supplier == null)
                return NotFound();

            return View("EditSupplier", supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                _context.Suppliers.Update(supplier);
                _context.SaveChanges();
                return RedirectToAction(nameof(SupplierIndex));
            }

            return View("EditSupplier", supplier);
        }

        // =========================
        // TOGGLE ACTIVE / INACTIVE
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleStatus(int id)
        {
            var supplier = _context.Suppliers.Find(id);
            if (supplier != null)
            {
                supplier.SupplierStatus =
                    supplier.SupplierStatus == "Active" ? "Inactive" : "Active";

                _context.SaveChanges();
            }

            return RedirectToAction(nameof(SupplierIndex));
        }

        // =========================
        // HARD DELETE
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var supplier = _context.Suppliers.Find(id);
            if (supplier == null)
                return NotFound();

            // 🔒 BLOCK DELETE IF SUPPLIER USED IN STOCK HISTORY
            bool hasStockHistory = _context.StockBatches
                .Any(b => b.SupplierNameSnapshot == supplier.SupplierName);

            if (hasStockHistory)
            {
                TempData["Error"] = "Cannot delete — supplier has stock transactions.";
                return RedirectToAction(nameof(SupplierIndex));
            }

            _context.Suppliers.Remove(supplier);
            _context.SaveChanges();

            TempData["Success"] = "Supplier deleted.";
            return RedirectToAction(nameof(SupplierIndex));
        }


        // =========================
        // JSON LIST (for dropdown refresh)
        // =========================
        [HttpGet]
        public IActionResult ListJson()
        {
            var suppliers = _context.Suppliers
                .Where(s => s.SupplierStatus == "Active") // optional but recommended
                .OrderBy(s => s.SupplierName)
                .Select(s => new
                {
                    id = s.SupplierID,
                    name = s.SupplierName
                })
                .ToList();

            return Json(suppliers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please upload a valid Excel (.xlsx) or CSV file.";
                return RedirectToAction(nameof(SupplierIndex));
            }

            var newSuppliers = new List<Supplier>();
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            // ======================
            // CSV IMPORT
            // ======================
            if (extension == ".csv")
            {
                using var reader = new StreamReader(file.OpenReadStream(), true);
                bool isHeader = true;

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (isHeader)
                    {
                        isHeader = false;
                        continue;
                    }

                    var cols = line.Split(',');
                    if (cols.Length < 5) continue;

                    var name = cols[0].Trim().TrimStart('\uFEFF');
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    bool exists = await _context.Suppliers
                        .AnyAsync(s => s.SupplierName == name);

                    if (exists) continue;

                    newSuppliers.Add(new Supplier
                    {
                        SupplierName = name,
                        SupplierContactPerson = cols[1].Trim(),
                        SupplierPhone = cols[2].Trim(),
                        SupplierEmail = cols[3].Trim(),
                        SupplierLeadTimeDays =
                            int.TryParse(cols[4], out var lead) ? lead : 0,
                        SupplierStatus =
                            cols.Length > 5 && !string.IsNullOrWhiteSpace(cols[5])
                                ? cols[5].Trim()
                                : "Active"
                    });
                }
            }

            // ======================
            // XLSX IMPORT (EPPlus)
            // ======================
            else if (extension == ".xlsx")
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using var package = new ExcelPackage(stream);
                var sheet = package.Workbook.Worksheets.FirstOrDefault();

                if (sheet?.Dimension == null)
                {
                    TempData["Error"] = "Excel file is empty.";
                    return RedirectToAction(nameof(SupplierIndex));
                }

                int rows = sheet.Dimension.Rows;

                for (int row = 2; row <= rows; row++)
                {
                    var name = sheet.Cells[row, 1].Text.Trim();
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    bool exists = await _context.Suppliers
                        .AnyAsync(s => s.SupplierName == name);

                    if (exists) continue;

                    newSuppliers.Add(new Supplier
                    {
                        SupplierName = name,
                        SupplierContactPerson = sheet.Cells[row, 2].Text.Trim(),
                        SupplierPhone = sheet.Cells[row, 3].Text.Trim(),
                        SupplierEmail = sheet.Cells[row, 4].Text.Trim(),
                        SupplierLeadTimeDays =
                            int.TryParse(sheet.Cells[row, 5].Text, out var lead) ? lead : 0,
                        SupplierStatus =
                            string.IsNullOrWhiteSpace(sheet.Cells[row, 6].Text)
                                ? "Active"
                                : sheet.Cells[row, 6].Text.Trim()
                    });
                }
            }
            else
            {
                TempData["Error"] = "Unsupported file type.";
                return RedirectToAction(nameof(SupplierIndex));
            }

            if (newSuppliers.Any())
            {
                _context.Suppliers.AddRange(newSuppliers);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"{newSuppliers.Count} suppliers imported successfully.";
            return RedirectToAction(nameof(SupplierIndex));
        }
        [HttpGet]
        public IActionResult Export()
        {
            var suppliers = _context.Suppliers
                .OrderBy(s => s.SupplierID)
                .AsNoTracking()
                .ToList();

            if (!suppliers.Any())
            {
                TempData["Error"] = "No supplier data found to export.";
                return RedirectToAction(nameof(SupplierIndex));
            }

            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Suppliers");

            // Header
            sheet.Cells[1, 1].Value = "SupplierID";
            sheet.Cells[1, 2].Value = "SupplierName";
            sheet.Cells[1, 3].Value = "ContactPerson";
            sheet.Cells[1, 4].Value = "Phone";
            sheet.Cells[1, 5].Value = "Email";
            sheet.Cells[1, 6].Value = "LeadTimeDays";
            sheet.Cells[1, 7].Value = "Status";

            sheet.Cells[1, 1, 1, 7].Style.Font.Bold = true;

            int row = 2;
            foreach (var s in suppliers)
            {
                sheet.Cells[row, 1].Value = s.SupplierID;
                sheet.Cells[row, 2].Value = s.SupplierName ?? "";
                sheet.Cells[row, 3].Value = s.SupplierContactPerson ?? "";
                sheet.Cells[row, 4].Value = s.SupplierPhone ?? "";
                sheet.Cells[row, 5].Value = s.SupplierEmail ?? "";
                sheet.Cells[row, 6].Value = s.SupplierLeadTimeDays;
                sheet.Cells[row, 7].Value = s.SupplierStatus ?? "";
                row++;
            }

            sheet.Cells.AutoFitColumns();

            return File(
                package.GetAsByteArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Suppliers_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            );
        }

    }
}

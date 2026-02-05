using Invexaaa.Data;
using Invexaaa.Models.Invexa;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Invexaaa.Controllers
{
    public class CustomerController : Controller
    {
        private readonly InvexaDbContext _context;

        public CustomerController(InvexaDbContext context)
        {
            _context = context;
        }

        // =========================
        // INDEX
        // =========================
        public IActionResult Index()
        {
            var list = _context.Customers
                .OrderBy(c => c.CustomerName)
                .ToList();

            return View("CustomerIndex", list); // 👈 explicit view
        }

        // =========================
        // CREATE (GET)
        // =========================
        public IActionResult Create()
        {
            return View("CreateCustomer", new Customer()); // 👈 explicit view
        }

        // =========================
        // CREATE (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Customer model, string? submitAction)
        {
            if (!ModelState.IsValid)
                return View("CreateCustomer", model);

            model.CustomerCreatedAt = DateTime.Now;

            _context.Customers.Add(model);
            _context.SaveChanges();

            TempData["Success"] = "Customer created successfully.";

            return submitAction == "saveNew"
                ? RedirectToAction(nameof(Create))
                : RedirectToAction(nameof(Index));
        }


        // =========================
        // EDIT (GET)
        // =========================
        public IActionResult Edit(int id)
        {
            var customer = _context.Customers.Find(id);
            if (customer == null)
                return NotFound();

            return View("EditCustomer", customer); // 👈 explicit view
        }

        // =========================
        // EDIT (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Customer model)
        {
            if (!ModelState.IsValid)
                return View("EditCustomer", model); // 👈 explicit view

            var customer = _context.Customers.Find(model.CustomerID);
            if (customer == null)
                return NotFound();

            customer.CustomerName = model.CustomerName;
            customer.CustomerPhone = model.CustomerPhone;
            customer.CustomerEmail = model.CustomerEmail;
            customer.CustomerAddress = model.CustomerAddress;
            customer.CustomerStatus = model.CustomerStatus;

            _context.SaveChanges();

            TempData["Success"] = "Customer updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // TOGGLE STATUS
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleStatus(int id)
        {
            var customer = _context.Customers.Find(id);
            if (customer == null)
                return NotFound();

            customer.CustomerStatus =
                customer.CustomerStatus == "Active" ? "Inactive" : "Active";

            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DELETE
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var customer = _context.Customers.Find(id);
            if (customer == null)
                return NotFound();

            bool hasTransactions = _context.StockTransactions
                .Any(t => t.CustomerID == id);

            if (hasTransactions)
            {
                TempData["Error"] = "Cannot delete — customer has stock transactions.";
                return RedirectToAction(nameof(Index));
            }

            _context.Customers.Remove(customer);
            _context.SaveChanges();

            TempData["Success"] = "Customer deleted.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // BULK DELETE
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BulkDelete(string ids)
        {
            if (string.IsNullOrWhiteSpace(ids))
                return RedirectToAction(nameof(Index));

            var idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(int.Parse)
                            .ToList();

            bool anyUsed = _context.StockTransactions
                .Any(t => t.CustomerID != null && idList.Contains(t.CustomerID.Value));

            if (anyUsed)
            {
                TempData["Error"] = "Cannot delete — one or more customers have stock transactions.";
                return RedirectToAction(nameof(Index));
            }

            var customers = _context.Customers
                .Where(c => idList.Contains(c.CustomerID))
                .ToList();

            _context.Customers.RemoveRange(customers);
            _context.SaveChanges();

            TempData["Success"] = $"{customers.Count} customer(s) deleted.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // IMPORT (CSV / XLSX)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please upload a valid Excel (.xlsx) or CSV file.";
                return RedirectToAction(nameof(Index));
            }

            var newCustomers = new List<Customer>();
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
                    if (cols.Length < 3) continue;

                    var name = cols[0].Trim().TrimStart('\uFEFF'); // remove BOM
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    bool exists = await _context.Customers
                        .AnyAsync(c => c.CustomerName == name);

                    if (exists) continue;

                    newCustomers.Add(new Customer
                    {
                        CustomerName = name,
                        CustomerPhone = cols.Length > 1 ? cols[1].Trim() : "",
                        CustomerEmail = cols.Length > 2 ? cols[2].Trim() : "",
                        CustomerAddress = cols.Length > 3 ? cols[3].Trim() : "",
                        CustomerStatus = (cols.Length > 4 && !string.IsNullOrWhiteSpace(cols[4]))
                                            ? cols[4].Trim()
                                            : "Active",
                        CustomerCreatedAt = DateTime.Now
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
                    return RedirectToAction(nameof(Index));
                }

                int rows = sheet.Dimension.Rows;

                // Columns:
                // 1 Name | 2 Phone | 3 Email | 4 Address | 5 Status
                for (int row = 2; row <= rows; row++)
                {
                    var name = sheet.Cells[row, 1].Text.Trim();
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    bool exists = await _context.Customers
                        .AnyAsync(c => c.CustomerName == name);

                    if (exists) continue;

                    newCustomers.Add(new Customer
                    {
                        CustomerName = name,
                        CustomerPhone = sheet.Cells[row, 2].Text.Trim(),
                        CustomerEmail = sheet.Cells[row, 3].Text.Trim(),
                        CustomerAddress = sheet.Cells[row, 4].Text.Trim(),
                        CustomerStatus = string.IsNullOrWhiteSpace(sheet.Cells[row, 5].Text)
                                            ? "Active"
                                            : sheet.Cells[row, 5].Text.Trim(),
                        CustomerCreatedAt = DateTime.Now
                    });
                }
            }
            else
            {
                TempData["Error"] = "Unsupported file type.";
                return RedirectToAction(nameof(Index));
            }

            if (newCustomers.Any())
            {
                _context.Customers.AddRange(newCustomers);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"{newCustomers.Count} customer(s) imported successfully.";
            return RedirectToAction(nameof(Index));
        }


        // =========================
        // EXPORT (XLSX)
        // =========================
        [HttpGet]
        public IActionResult Export()
        {
            var customers = _context.Customers
                .OrderBy(c => c.CustomerID)
                .AsNoTracking()
                .ToList();

            if (!customers.Any())
            {
                TempData["Error"] = "No customer data found to export.";
                return RedirectToAction(nameof(Index));
            }

            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Customers");

            // Header
            sheet.Cells[1, 1].Value = "CustomerID";
            sheet.Cells[1, 2].Value = "CustomerName";
            sheet.Cells[1, 3].Value = "Phone";
            sheet.Cells[1, 4].Value = "Email";
            sheet.Cells[1, 5].Value = "Address";
            sheet.Cells[1, 6].Value = "Status";
            sheet.Cells[1, 7].Value = "CreatedAt";

            sheet.Cells[1, 1, 1, 7].Style.Font.Bold = true;

            int row = 2;
            foreach (var c in customers)
            {
                sheet.Cells[row, 1].Value = c.CustomerID;
                sheet.Cells[row, 2].Value = c.CustomerName ?? "";
                sheet.Cells[row, 3].Value = c.CustomerPhone ?? "";
                sheet.Cells[row, 4].Value = c.CustomerEmail ?? "";
                sheet.Cells[row, 5].Value = c.CustomerAddress ?? "";
                sheet.Cells[row, 6].Value = c.CustomerStatus ?? "";
                sheet.Cells[row, 7].Value = c.CustomerCreatedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                row++;
            }

            sheet.Cells.AutoFitColumns();

            return File(
                package.GetAsByteArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Customers_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            );
        }

    }
}

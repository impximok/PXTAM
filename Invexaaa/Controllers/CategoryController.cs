using Invexaaa.Data;
using Invexaaa.Models.Invexa;
using Invexaaa.Models.Invexa.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using QuestPDF.Infrastructure;
using System.Globalization;


namespace Invexaaa.Controllers
{
    public class CategoryController : Controller
    {
        private readonly InvexaDbContext _context;

        public CategoryController(InvexaDbContext context)
        {
            _context = context;
        }

        // =========================
        // LIST
        // =========================
        public IActionResult CategoryIndex()
        {

            var categories = _context.Categories.ToList();
            return View("CategoryIndex", categories);
        }

        // =========================
        // CREATE
        // =========================
        public IActionResult Create()
        {
            return View("CreateCategory");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category category, string submitAction)
        {

            // ✅ Force CostingMethod selection (block default / empty)
            // Adjust "FIFO" if FIFO is your enum default/0
            

            if (!ModelState.IsValid)
                return View("CreateCategory", category);

            submitAction ??= "save"; // optional

            _context.Categories.Add(category);
            _context.SaveChanges();

            if (submitAction == "saveNew")
                return RedirectToAction(nameof(Create));

            return RedirectToAction(nameof(CategoryIndex));
        }


        // =========================
        // EDIT
        // =========================
        public IActionResult Edit(int id)
        {
            var category = _context.Categories.Find(id);
            if (category == null)
                return NotFound();

            return View("EditCategory", category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Categories.Update(category);
                _context.SaveChanges();
                return RedirectToAction(nameof(CategoryIndex));
            }

            return View("EditCategory", category);
        }

        // =========================
        // TOGGLE ACTIVE / INACTIVE
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleStatus(int id)
        {
            var category = _context.Categories.Find(id);
            if (category != null)
            {
                category.CategoryStatus =
                    category.CategoryStatus == "Active" ? "Inactive" : "Active";

                _context.SaveChanges();
            }

            return RedirectToAction(nameof(CategoryIndex));
        }

        // =========================
        // HARD DELETE
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var category = _context.Categories.FirstOrDefault(c => c.CategoryID == id);
            if (category == null)
                return NotFound();

            bool hasItems = _context.Items.Any(i => i.CategoryID == id);

            if (hasItems)
            {
                TempData["Error"] = "Cannot delete category because it is used by items.";
                return RedirectToAction(nameof(CategoryIndex));
            }

            _context.Categories.Remove(category);
            _context.SaveChanges();

            TempData["Success"] = "Category deleted successfully.";
            return RedirectToAction(nameof(CategoryIndex));
        }


        // =========================
        // JSON LIST (for dropdown refresh)
        // =========================
        [HttpGet]
        public IActionResult ListJson()
        {
            var categories = _context.Categories
                .Where(c => c.CategoryStatus == "Active") // optional but recommended
                .OrderBy(c => c.CategoryName)
                .Select(c => new
                {
                    id = c.CategoryID,
                    name = c.CategoryName
                })
                .ToList();

            return Json(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete(string ids)
        {
            if (string.IsNullOrWhiteSpace(ids))
                return RedirectToAction(nameof(CategoryIndex));


            var idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => int.Parse(x))
                            .ToList();

            var categories = await _context.Categories
                .Where(c => idList.Contains(c.CategoryID))
                .ToListAsync();

            _context.Categories.RemoveRange(categories);
            await _context.SaveChangesAsync();

            TempData["Info"] = $"{categories.Count} categories deleted.";
            return RedirectToAction(nameof(CategoryIndex));

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please upload a valid Excel (.xlsx) or CSV file.";
                return RedirectToAction(nameof(CategoryIndex));
            }

            var newCategories = new List<Category>();
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            // ======================
            // CSV IMPORT (UTF-8 SAFE)
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

                    if (cols.Length < 4) continue;

                    var name = cols[0].Trim().TrimStart('\uFEFF'); // BOM FIX
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    bool exists = await _context.Categories
                        .AnyAsync(c => c.CategoryName == name);

                    if (exists) continue;

                    newCategories.Add(new Category
                    {
                        CategoryName = name,
                        CategoryDescription = cols[1].Trim(),
                        CostingMethod = ParseCostingMethod(cols[2]),
                        CategoryStatus = string.IsNullOrWhiteSpace(cols[3]) ? "Active" : cols[3].Trim()
                    });
                }
            }

            // ======================
            // XLSX IMPORT (EPPlus 8)
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
                    return RedirectToAction(nameof(CategoryIndex));
                }

                int rows = sheet.Dimension.Rows;

                for (int row = 2; row <= rows; row++)
                {
                    var name = sheet.Cells[row, 1].Text.Trim();
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    bool exists = await _context.Categories
                        .AnyAsync(c => c.CategoryName == name);

                    if (exists) continue;

                    newCategories.Add(new Category
                    {
                        CategoryName = name,
                        CategoryDescription = sheet.Cells[row, 2].Text.Trim(),
                        CostingMethod = ParseCostingMethod(sheet.Cells[row, 3].Text),
                        CategoryStatus = string.IsNullOrWhiteSpace(sheet.Cells[row, 4].Text)
                            ? "Active"
                            : sheet.Cells[row, 4].Text.Trim()
                    });
                }
            }
            else
            {
                TempData["Error"] = "Unsupported file type.";
                return RedirectToAction(nameof(CategoryIndex));
            }

            if (newCategories.Any())
            {
                _context.Categories.AddRange(newCategories);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"{newCategories.Count} categories imported successfully.";
            return RedirectToAction(nameof(CategoryIndex));
        }



        private CostingMethod ParseCostingMethod(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return CostingMethod.FIFO; // default

            value = value.Trim();

            // Try numeric (1,2,3)
            if (int.TryParse(value, out int number) &&
                Enum.IsDefined(typeof(CostingMethod), number))
            {
                return (CostingMethod)number;
            }

            // Try text (FIFO, Weighted Average, Fixed)
            value = value.Replace(" ", "");

            if (Enum.TryParse(value, true, out CostingMethod method))
            {
                return method;
            }

            // Fallback
            return CostingMethod.FIFO;
        }

        [HttpGet]
        public IActionResult Export()
        {
            var categories = _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.CategoryID)
                .ToList();

            if (!categories.Any())
            {
                TempData["Error"] = "No category data found to export.";
                return RedirectToAction(nameof(CategoryIndex));
            }

            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Categories");

            // Header
            sheet.Cells[1, 1].Value = "CategoryID";
            sheet.Cells[1, 2].Value = "CategoryName";
            sheet.Cells[1, 3].Value = "CategoryDescription";
            sheet.Cells[1, 4].Value = "CostingMethod";
            sheet.Cells[1, 5].Value = "CategoryStatus";

            sheet.Cells[1, 1, 1, 5].Style.Font.Bold = true;

            int row = 2;
            foreach (var c in categories)
            {
                sheet.Cells[row, 1].Value = c.CategoryID;
                sheet.Cells[row, 2].Value = c.CategoryName ?? "";
                sheet.Cells[row, 3].Value = c.CategoryDescription ?? "";
                sheet.Cells[row, 4].Value = c.CostingMethod.ToString();
                sheet.Cells[row, 5].Value = c.CategoryStatus ?? "";
                row++;
            }

            sheet.Cells.AutoFitColumns();

            return File(
                package.GetAsByteArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Categories_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            );
        }



    }
}

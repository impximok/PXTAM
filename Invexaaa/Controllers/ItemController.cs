using Invexaaa.Data;
using Invexaaa.Models.Invexa;
using Invexaaa.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Text;
using ZXing;
using ZXing.Common;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Globalization;

namespace Invexaaa.Controllers
{
    [Route("Item")] // 👈 IMPORTANT: fixes routing ambiguity
    public class ItemController : Controller
    {
        private readonly InvexaDbContext _context;

        public ItemController(InvexaDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // ITEM INDEX (CARD GRID)
        // Admin, Manager, Staff
        // URL: /Item/ItemIndex
        // View: Views/Item/ItemIndex.cshtml
        // =====================================================
        [Authorize(Roles = "Admin,Manager,Staff")]
        [HttpGet("ItemIndex")]
        public IActionResult ItemIndex(string search, int? categoryId, string status, int page = 1, int pageSize = 12)
        {
            var items =
    from i in _context.Items

    join c in _context.Categories
        on i.CategoryID equals c.CategoryID



    // ✅ LEFT JOIN Inventory (CRITICAL FIX)
    join inv in _context.Inventories
        on i.ItemID equals inv.ItemID into invGroup
    from inv in invGroup.DefaultIfEmpty()

    select new ItemCardViewModel
    {
        ItemID = i.ItemID,
        ItemName = i.ItemName,

        CategoryID = i.CategoryID,
        CategoryName = c.CategoryName,


        ItemSellPrice = i.ItemSellPrice,
        ItemStatus = i.ItemStatus,

        // ✅ Image now ALWAYS reaches the view
        ItemImageUrl = i.ItemImageUrl,

        ReorderLevel = i.ItemReorderLevel,
        SafetyStock = i.SafetyStock,

        // ✅ Inventory-safe (null-proof)
        CurrentBalance = inv != null ? inv.InventoryTotalQuantity : 0,

        ItemBarcode = i.ItemBarcode
    };


            // ✅ Filters
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                items = items.Where(x =>
                    x.ItemName.Contains(term) ||
                    (x.ItemBarcode != null && x.ItemBarcode.Contains(term))
                );
            }

            if (categoryId.HasValue)
                items = items.Where(x => x.CategoryID == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(status))
                items = items.Where(x => x.ItemStatus == status);

            // ✅ ViewBag for dropdowns
            ViewBag.Categories = _context.Categories
                .Where(c => c.CategoryStatus == "Active")
                .ToList();

            ViewBag.StatusList = new[] { "Active", "Inactive" };

            // ✅ Paging (AFTER filters)
            var totalCount = items.Count();
            if (page < 1) page = 1;

            var pagedItems = items
                .OrderBy(x => x.ItemID)              // stable ordering
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;

            return View("ItemIndex", pagedItems);
        }



        // =====================================================
        // ITEM DETAIL
        // URL: /Item/ItemDetail/5
        // =====================================================
        [Authorize(Roles = "Admin,Manager,Staff")]
        [HttpGet("ItemDetail/{id}")]
        public IActionResult ItemDetail(int id)
        {
            var item =
    (from i in _context.Items
     join c in _context.Categories on i.CategoryID equals c.CategoryID

     join inv in _context.Inventories on i.ItemID equals inv.ItemID
     where i.ItemID == id
     select new ItemDetailViewModel
     {
         ItemID = i.ItemID,
         ItemName = i.ItemName,
         CategoryName = c.CategoryName,
   

         UnitOfMeasure = i.ItemUnitOfMeasure,
         BuyPrice = i.ItemBuyPrice,
         SellPrice = i.ItemSellPrice,

         ReorderLevel = i.ItemReorderLevel,
         SafetyStock = i.SafetyStock,

         Status = i.ItemStatus,
         CreatedDate = i.ItemCreatedDate,
         ImageUrl = i.ItemImageUrl,

         CurrentBalance = inv.InventoryTotalQuantity,
         ItemBarcode = i.ItemBarcode
     }).FirstOrDefault();


            if (item == null)
                return NotFound();

            item.Batches =
    _context.StockBatches
    .Where(b => b.ItemID == id)
    .Select(b => new ItemBatchViewModel
    {
        BatchNo = b.BatchNumber,
        Quantity = b.BatchQuantity,
        ExpiryDate = b.BatchExpiryDate,

        ExpiryStatus =
            b.BatchExpiryDate < DateTime.Today ? "Expired" :
            b.BatchExpiryDate <= DateTime.Today.AddDays(30) ? "Near Expiry" :
            "Safe"
    })
    .ToList();


            return View("ItemDetail", item);
        }

        // =====================================================
        // CREATE ITEM (GET)
        // URL: /Item/CreateItem
        // =====================================================
        [Authorize(Roles = "Admin,Manager")]
        [HttpGet("CreateItem")]
        public IActionResult CreateItem()
        {
            return View("CreateItem", new ItemFormViewModel
            {
                Categories = _context.Categories
                    .Where(c => c.CategoryStatus == "Active")
                    .ToList(),

            });
        }

        // =====================================================
        // CREATE ITEM (POST)
        // =====================================================
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost("CreateItem")]
        [ValidateAntiForgeryToken]
        public IActionResult CreateItem(ItemFormViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Categories = _context.Categories.ToList();

                return View("CreateItem", vm);
            }
            // ================= IMAGE UPLOAD =================
            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/uploads/items"
                );

                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(vm.ImageFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    vm.ImageFile.CopyTo(stream);
                }

                // ✅ VERY IMPORTANT: save WEB path, not physical path
                vm.Item.ItemImageUrl = $"/uploads/items/{fileName}";
            }

            // =====================================================
            // ⭐ AUTO-GENERATE BARCODE (Option B)
            // =====================================================

            // Get last barcode
            var lastBarcode = _context.Items
                .Where(i => i.ItemBarcode != null)
                .OrderByDescending(i => i.ItemID)
                .Select(i => i.ItemBarcode)
                .FirstOrDefault();

            int nextNumber = 1;

            if (!string.IsNullOrEmpty(lastBarcode))
            {
                // Example: INVX-000123
                var numberPart = lastBarcode.Split('-')[1];
                nextNumber = int.Parse(numberPart) + 1;
            }

            // Generate new barcode
            vm.Item.ItemBarcode = $"INVX-{nextNumber:D6}";


            // =====================================================
            // SAVE ITEM
            // =====================================================
            _context.Items.Add(vm.Item);
            _context.SaveChanges();

            // Create inventory row
            _context.Inventories.Add(new Inventory
            {
                ItemID = vm.Item.ItemID,
                InventoryTotalQuantity = 0
            });

            _context.SaveChanges();

            return RedirectToAction("ItemIndex", "Item");
        }


        // =====================================================
        // EDIT ITEM
        // =====================================================
        [Authorize(Roles = "Admin,Manager")]
        [HttpGet("EditItem/{id}")]
        public IActionResult EditItem(int id)

        {
            var item = _context.Items.Find(id);
            if (item == null) return NotFound();

            var vm = new ItemFormViewModel
            {
                Item = item,
                Categories = _context.Categories.ToList(),
    
            };

            return View(vm);
        }


        [Authorize(Roles = "Admin,Manager")]
        [HttpPost("EditItem/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult EditItem(ItemFormViewModel model)

        {
            if (!ModelState.IsValid)
            {
                // 🔴 IMPORTANT: reload dropdown data
                model.Categories = _context.Categories.ToList();
            
                return View(model);
            }

            var item = _context.Items.Find(model.Item.ItemID);
            if (item == null) return NotFound();

            // ✅ Update fields
            item.ItemName = model.Item.ItemName;
            item.ItemDescription = model.Item.ItemDescription;
            item.ItemUnitOfMeasure = model.Item.ItemUnitOfMeasure;
            item.CategoryID = model.Item.CategoryID;

            item.ItemBuyPrice = model.Item.ItemBuyPrice;
            item.ItemSellPrice = model.Item.ItemSellPrice;
            item.ItemReorderLevel = model.Item.ItemReorderLevel;
            item.SafetyStock = model.Item.SafetyStock;
            item.ReorderPoint = model.Item.ReorderPoint;
            item.AverageDailyDemand = model.Item.AverageDailyDemand;

            // ❗ DO NOT TOUCH barcode
            // ❗ DO NOT TOUCH ItemID

            // ================= IMAGE UPDATE =================
            if (!string.IsNullOrEmpty(model.EditedImageData))
            {
                // Edited image from canvas (base64)
                var base64 = model.EditedImageData;

                // Remove prefix: data:image/png;base64,...
                var commaIndex = base64.IndexOf(',');
                if (commaIndex >= 0)
                    base64 = base64[(commaIndex + 1)..];

                byte[] imageBytes = Convert.FromBase64String(base64);

                var uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/uploads/items"
                );

                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}.png";
                var filePath = Path.Combine(uploadsFolder, fileName);

                System.IO.File.WriteAllBytes(filePath, imageBytes);

                // OPTIONAL: delete old image file
                if (!string.IsNullOrEmpty(item.ItemImageUrl))
                {
                    var oldPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        item.ItemImageUrl.TrimStart('/')
                    );

                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                // SAVE WEB PATH
                item.ItemImageUrl = $"/uploads/items/{fileName}";
            }


            _context.SaveChanges();

            return RedirectToAction("ItemIndex");
        }

        // SOFT DELETE = DEACTIVATE
        [Authorize(Roles = "Admin,Manager")]
        [HttpGet("DeactivateItem/{id}")]
        public IActionResult DeactivateItem(int id)
        {
            var item = _context.Items.FirstOrDefault(i => i.ItemID == id);
            if (item == null) return NotFound();

            var categoryName = _context.Categories
                .Where(c => c.CategoryID == item.CategoryID)
                .Select(c => c.CategoryName)
                .FirstOrDefault();

            ViewBag.CategoryName = categoryName;

            return View("DeactivateItem", item);
        }



        [Authorize(Roles = "Admin,Manager")]
        [HttpPost("DeactivateItem/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult DeactivateItemConfirmed(int id)
        {
            var item = _context.Items.Find(id);
            if (item == null) return NotFound();

            item.ItemStatus = "Inactive";
            _context.SaveChanges();

            return RedirectToAction("ItemIndex");
        }

        // =====================================================
        // ACTIVATE ITEM (CONFIRMATION)
        // =====================================================
        [Authorize(Roles = "Admin,Manager")]
        [HttpGet("ActivateItem/{id}")]
        public IActionResult ActivateItem(int id)
        {
            var item = _context.Items.FirstOrDefault(i => i.ItemID == id);
            if (item == null) return NotFound();

            var categoryName = _context.Categories
                .Where(c => c.CategoryID == item.CategoryID)
                .Select(c => c.CategoryName)
                .FirstOrDefault();

            ViewBag.CategoryName = categoryName;

            return View("ActivateItem", item);
        }


        // =====================================================
        // ACTIVATE ITEM (POST)
        // =====================================================
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost("ActivateItem/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult ActivateItemConfirmed(int id)
        {
            var item = _context.Items.Find(id);
            if (item == null) return NotFound();

            item.ItemStatus = "Active";
            _context.SaveChanges();

            TempData["Success"] = "Item has been activated.";

            return RedirectToAction("ItemIndex");
        }



        // =====================================================
        // FORCE DELETE CONFIRMATION (GET)
        // Admin only
        // URL: /Item/ForceDeleteConfirm/{id}
        // =====================================================
        [Authorize(Roles = "Admin")]
        [HttpGet("ForceDeleteConfirm/{id}")]
        public IActionResult ForceDeleteConfirm(int id)
        {
            var item = _context.Items.Find(id);
            if (item == null) return NotFound();

            return View("ForceDeleteConfirm", item);
        }



        [Authorize(Roles = "Admin")]
        [HttpPost("ForceDeleteItem/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult ForceDeleteItem(int id)
        {
            using var tx = _context.Database.BeginTransaction();

            try
            {
                var item = _context.Items.FirstOrDefault(i => i.ItemID == id);
                if (item == null)
                    return NotFound();

                // =====================================================
                // 1️⃣ DELETE STOCK TRANSACTIONS
                // =====================================================
                var transactions = _context.StockTransactions
                    .Where(t => t.ItemID == id)
                    .ToList();
                _context.StockTransactions.RemoveRange(transactions);

                // =====================================================
                // 2️⃣ DELETE STOCK ADJUSTMENT DETAILS
                // =====================================================
                var adjustmentDetails = _context.StockAdjustmentDetails
                    .Where(d => d.ItemID == id)
                    .ToList();
                _context.StockAdjustmentDetails.RemoveRange(adjustmentDetails);

                // =====================================================
                // 3️⃣ DELETE STOCK ADJUSTMENTS (ORPHANS)
                // =====================================================
                var adjustmentIds = adjustmentDetails
                    .Select(d => d.AdjustmentID)
                    .Distinct()
                    .ToList();

                var adjustments = _context.StockAdjustments
                    .Where(a => adjustmentIds.Contains(a.AdjustmentID))
                    .ToList();
                _context.StockAdjustments.RemoveRange(adjustments);

                // =====================================================
                // 4️⃣ DELETE STOCK BATCHES
                // =====================================================
                var batches = _context.StockBatches
                    .Where(b => b.ItemID == id)
                    .ToList();
                _context.StockBatches.RemoveRange(batches);

                // =====================================================
                // 5️⃣ DELETE INVENTORY
                // =====================================================
                var inventory = _context.Inventories
                    .FirstOrDefault(inv => inv.ItemID == id);
                if (inventory != null)
                    _context.Inventories.Remove(inventory);

                // =====================================================
                // 6️⃣ DELETE ITEM (LAST!)
                // =====================================================
                _context.Items.Remove(item);

                _context.SaveChanges();
                tx.Commit();

                TempData["Success"] = "Item permanently deleted from database.";
                return RedirectToAction("ItemIndex");
            }
            catch (Exception ex)
            {
                tx.Rollback();
                throw;
            }
        }



        [Authorize(Roles = "Admin,Manager,Staff")]
        [HttpGet("Scan")]
        public IActionResult Scan(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest();

            var itemId = _context.Items
                .Where(i => i.ItemBarcode == code)
                .Select(i => i.ItemID)
                .FirstOrDefault();

            if (itemId == 0)
                return NotFound("Item not found");

            return RedirectToAction("ItemDetail", new { id = itemId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please upload a valid Excel (.xlsx) or CSV file.";
                return RedirectToAction(nameof(ItemIndex));
            }

            var newItems = new List<Item>();
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

                    if (cols.Length < 6) continue;

                    var itemName = cols[0].Trim().TrimStart('\uFEFF');
                    var categoryName = cols[1].Trim();

                    if (string.IsNullOrWhiteSpace(itemName)) continue;

                    bool exists = await _context.Items
                        .AnyAsync(i => i.ItemName == itemName);

                    if (exists) continue;

                    var category = await _context.Categories
                        .FirstOrDefaultAsync(c => c.CategoryName == categoryName);

                    if (category == null) continue;

                    newItems.Add(new Item
                    {
                        ItemName = itemName,
                        CategoryID = category.CategoryID,
                        ItemUnitOfMeasure = cols[2].Trim(),
                        ItemBuyPrice = decimal.TryParse(cols[3], out var buy) ? buy : 0,
                        ItemSellPrice = decimal.TryParse(cols[4], out var sell) ? sell : 0,
                        ItemStatus = string.IsNullOrWhiteSpace(cols[5]) ? "Active" : cols[5].Trim(),
                        ItemCreatedDate = DateTime.Now,
                        ItemImageUrl = "/images/items/item-default.png"
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
                    return RedirectToAction(nameof(ItemIndex));
                }

                int rows = sheet.Dimension.Rows;

                for (int row = 2; row <= rows; row++)
                {
                    var itemName = sheet.Cells[row, 1].Text.Trim();
                    var categoryName = sheet.Cells[row, 2].Text.Trim();

                    if (string.IsNullOrWhiteSpace(itemName)) continue;

                    bool exists = await _context.Items
                        .AnyAsync(i => i.ItemName == itemName);

                    if (exists) continue;

                    var category = await _context.Categories
                        .FirstOrDefaultAsync(c => c.CategoryName == categoryName);

                    if (category == null) continue;

                    newItems.Add(new Item
                    {
                        ItemName = itemName,
                        CategoryID = category.CategoryID,
                        ItemUnitOfMeasure = sheet.Cells[row, 3].Text.Trim(),
                        ItemBuyPrice = decimal.TryParse(sheet.Cells[row, 4].Text, out var buy) ? buy : 0,
                        ItemSellPrice = decimal.TryParse(sheet.Cells[row, 5].Text, out var sell) ? sell : 0,
                        ItemReorderLevel = int.TryParse(sheet.Cells[row, 6].Text, out var rl) ? rl : 0,
                        SafetyStock = int.TryParse(sheet.Cells[row, 7].Text, out var ss) ? ss : 0,
                        ItemStatus = string.IsNullOrWhiteSpace(sheet.Cells[row, 8].Text)
                            ? "Active"
                            : sheet.Cells[row, 8].Text.Trim(),
                        ItemCreatedDate = DateTime.Now
                    });
                }
            }
            else
            {
                TempData["Error"] = "Unsupported file type.";
                return RedirectToAction(nameof(ItemIndex));
            }

            // =====================================================
            // ⭐ AUTO-GENERATE BARCODE FOR IMPORTED ITEMS
            // =====================================================
            var lastBarcode = await _context.Items
                .Where(i => i.ItemBarcode != null)
                .OrderByDescending(i => i.ItemID)
                .Select(i => i.ItemBarcode)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (!string.IsNullOrEmpty(lastBarcode))
            {
                // Expected format: INVX-000123
                var numberPart = lastBarcode.Split('-')[1];
                nextNumber = int.Parse(numberPart) + 1;
            }

            // Assign barcode to each imported item
            foreach (var item in newItems)
            {
                item.ItemBarcode = $"INVX-{nextNumber:D6}";
                nextNumber++;
            }


            if (newItems.Any())
            {
                _context.Items.AddRange(newItems);
                await _context.SaveChangesAsync();

                // Auto-create inventory rows
                var inventories = newItems.Select(i => new Inventory
                {
                    ItemID = i.ItemID,
                    InventoryTotalQuantity = 0
                });

                _context.Inventories.AddRange(inventories);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"{newItems.Count} items imported successfully.";
            return RedirectToAction(nameof(ItemIndex));
        }

        [HttpGet]
        public IActionResult Export()
        {
            var items =
    (from i in _context.Items
     join c in _context.Categories
         on i.CategoryID equals c.CategoryID
     orderby i.ItemID
     select new
     {
         i.ItemID,
         i.ItemName,
         CategoryName = c.CategoryName,
         i.ItemUnitOfMeasure,
         i.ItemBuyPrice,
         i.ItemSellPrice,
         i.ItemReorderLevel,
         i.SafetyStock,
         i.ItemStatus,
         i.ItemBarcode
     })
    .AsNoTracking()
    .ToList();


            if (!items.Any())
            {
                TempData["Error"] = "No item data found to export.";
                return RedirectToAction(nameof(ItemIndex));
            }

            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Items");

            // Header
            sheet.Cells[1, 1].Value = "ItemID";
            sheet.Cells[1, 2].Value = "ItemName";
            sheet.Cells[1, 3].Value = "CategoryName";
            sheet.Cells[1, 4].Value = "UnitOfMeasure";
            sheet.Cells[1, 5].Value = "BuyPrice";
            sheet.Cells[1, 6].Value = "SellPrice";
            sheet.Cells[1, 7].Value = "ReorderLevel";
            sheet.Cells[1, 8].Value = "SafetyStock";
            sheet.Cells[1, 9].Value = "ItemStatus";
            sheet.Cells[1, 10].Value = "Barcode";

            sheet.Cells[1, 1, 1, 10].Style.Font.Bold = true;

            int row = 2;
            foreach (var i in items)
            {
                sheet.Cells[row, 1].Value = i.ItemID;
                sheet.Cells[row, 2].Value = i.ItemName ?? "";
                sheet.Cells[row, 3].Value = i.CategoryName ?? "";
                sheet.Cells[row, 4].Value = i.ItemUnitOfMeasure ?? "";
                sheet.Cells[row, 5].Value = i.ItemBuyPrice;
                sheet.Cells[row, 6].Value = i.ItemSellPrice;
                sheet.Cells[row, 7].Value = i.ItemReorderLevel;
                sheet.Cells[row, 8].Value = i.SafetyStock;
                sheet.Cells[row, 9].Value = i.ItemStatus ?? "";
                sheet.Cells[row, 10].Value = i.ItemBarcode ?? "";
                row++;
            }

            sheet.Cells.AutoFitColumns();

            return File(
                package.GetAsByteArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Items_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            );
        }

    }
}

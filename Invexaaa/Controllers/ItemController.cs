using Invexaaa.Data;
using Invexaaa.Models.Invexa;
using Invexaaa.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Text;
using ZXing;
using ZXing.Common;

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

    join s in _context.Suppliers
        on i.SupplierID equals s.SupplierID

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

        SupplierName = s.SupplierName,

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
     join s in _context.Suppliers on i.SupplierID equals s.SupplierID
     join inv in _context.Inventories on i.ItemID equals inv.ItemID
     where i.ItemID == id
     select new ItemDetailViewModel
     {
         ItemID = i.ItemID,
         ItemName = i.ItemName,
         CategoryName = c.CategoryName,
         SupplierName = s.SupplierName,

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
                Suppliers = _context.Suppliers.ToList()
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
                vm.Suppliers = _context.Suppliers.ToList();
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
            if (item == null)
                return NotFound();

            return View("EditItem", new ItemFormViewModel
            {
                Item = item,
                Categories = _context.Categories.ToList(),
                Suppliers = _context.Suppliers.ToList()
            });
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost("EditItem")]
        [ValidateAntiForgeryToken]
        public IActionResult EditItem(ItemFormViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Categories = _context.Categories.ToList();
                vm.Suppliers = _context.Suppliers.ToList();
                return View("EditItem", vm);
            }

            var item = _context.Items.FirstOrDefault(i => i.ItemID == vm.Item.ItemID);
            if (item == null)
                return NotFound();

            // =====================================================
            // IMAGE: edited (Base64 from canvas)
            // =====================================================
            if (!string.IsNullOrEmpty(vm.EditedImageData))
            {
                var base64 = vm.EditedImageData.Split(',')[1];
                var bytes = Convert.FromBase64String(base64);

                var uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/uploads/items"
                );

                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}.png";
                var filePath = Path.Combine(uploadsFolder, fileName);

                System.IO.File.WriteAllBytes(filePath, bytes);

                item.ItemImageUrl = $"/uploads/items/{fileName}";
            }
            // =====================================================
            // IMAGE: normal file upload
            // =====================================================
            else if (vm.ImageFile != null && vm.ImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/uploads/items"
                );

                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(vm.ImageFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                vm.ImageFile.CopyTo(stream);

                item.ItemImageUrl = $"/uploads/items/{fileName}";
            }

            // =====================================================
            // UPDATE FIELDS (explicit = safe)
            // =====================================================
            item.ItemName = vm.Item.ItemName;
            item.ItemDescription = vm.Item.ItemDescription;
            item.ItemBuyPrice = vm.Item.ItemBuyPrice;
            item.ItemSellPrice = vm.Item.ItemSellPrice;
            item.ItemReorderLevel = vm.Item.ItemReorderLevel;
            item.SafetyStock = vm.Item.SafetyStock;
            item.ReorderPoint = vm.Item.ReorderPoint;
            item.AverageDailyDemand = vm.Item.AverageDailyDemand;

            // 🔥 REQUIRED: must come from hidden inputs
            item.CategoryID = vm.Item.CategoryID;
            item.SupplierID = vm.Item.SupplierID;

            _context.SaveChanges();

            return RedirectToAction("ItemDetail", new { id = item.ItemID });
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

    }
}

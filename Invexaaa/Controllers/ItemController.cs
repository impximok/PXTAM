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
        public IActionResult ItemIndex(string search, int? categoryId, string status)
        {
            var items =
                from i in _context.Items
                join c in _context.Categories on i.CategoryID equals c.CategoryID
                join s in _context.Suppliers on i.SupplierID equals s.SupplierID
                join inv in _context.Inventories on i.ItemID equals inv.ItemID
                select new ItemCardViewModel
                {
                    ItemID = i.ItemID,
                    ItemName = i.ItemName,
                    CategoryName = c.CategoryName,
                    SupplierName = s.SupplierName,

                    ItemSellPrice = i.ItemSellPrice,
                    ItemStatus = i.ItemStatus,
                    ItemImageUrl = i.ItemImageUrl,

                    ReorderLevel = i.ItemReorderLevel,
                    SafetyStock = i.SafetyStock,
                    CurrentBalance = inv.InventoryTotalQuantity,

                    ItemBarcode = i.ItemBarcode   // ✅ ADD THIS
                };


            if (!string.IsNullOrWhiteSpace(search))
                items = items.Where(x => x.ItemName.Contains(search));

            if (categoryId.HasValue)
                items = items.Where(x =>
                    _context.Items.Any(i => i.ItemID == x.ItemID && i.CategoryID == categoryId));

            if (!string.IsNullOrWhiteSpace(status))
                items = items.Where(x => x.ItemStatus == status);

            ViewBag.Categories = _context.Categories
                .Where(c => c.CategoryStatus == "Active")
                .ToList();

            ViewBag.StatusList = new[] { "Active", "Inactive" };

            // 👇 EXPLICIT VIEW NAME (important)
            return View("ItemIndex", items.ToList());
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
                    BatchStatus = b.BatchStatus
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

            _context.Items.Update(vm.Item);
            _context.SaveChanges();

            return RedirectToAction("ItemDetail", "Item", new { id = vm.Item.ItemID });
        }

        // =====================================================
        // DELETE ITEM (SOFT DELETE)
        // =====================================================
        [Authorize(Roles = "Admin,Manager")]
        [HttpGet("DeleteItem/{id}")]
        public IActionResult DeleteItem(int id)
        {
            var item = _context.Items.Find(id);
            if (item == null)
                return NotFound();

            return View("DeleteItem", item);
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost("DeleteItem")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteItemConfirmed(int id)
        {
            var item = _context.Items.Find(id);
            if (item == null)
                return NotFound();

            item.ItemStatus = "Inactive";
            _context.SaveChanges();

            return RedirectToAction("ItemIndex", "Item");
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

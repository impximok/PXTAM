using Microsoft.AspNetCore.Mvc;
using Invexaaa.Data;
using Invexaaa.Models.Invexa;
using Invexaaa.Models.ViewModels;

namespace Invexaaa.Controllers
{
    public class StockController : Controller
    {
        private readonly InvexaDbContext _context;

        public StockController(InvexaDbContext context)
        {
            _context = context;
        }

        // =========================
        // MANAGE STOCK (INDEX)
        // =========================
        public IActionResult StockIndex()
        {
            var stockList =
                from inv in _context.Inventories
                join item in _context.Items on inv.ItemID equals item.ItemID
                join cat in _context.Categories on item.CategoryID equals cat.CategoryID
                select new StockViewModel
                {
                    InventoryID = inv.InventoryID,
                    ItemID = item.ItemID,
                    ItemName = item.ItemName,
                    CategoryName = cat.CategoryName,
                    AvailableQuantity = inv.InventoryTotalQuantity,

                    ItemImageUrl = item.ItemImageUrl, // ✅ added

                    StockStatus =
                        inv.InventoryTotalQuantity == 0 ? "Out of Stock" :
                        inv.InventoryTotalQuantity <= 10 ? "Low Stock" :
                        "In Stock",

                    LastUpdated = inv.InventoryLastUpdated
                };

            return View("StockIndex", stockList.ToList());
        }



        // =========================
        // SINGLE ADJUST (GET)
        // =========================
        public IActionResult Adjust(int inventoryId)
        {
            var inv = _context.Inventories.Find(inventoryId);
            if (inv == null) return NotFound();

            return View("AdjustStock", inv);
        }

        // =========================
        // SINGLE ADJUST (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Adjust(int inventoryId, int newQuantity, string reason)
        {
            if (newQuantity < 0)
            {
                ModelState.AddModelError("", "Quantity cannot be negative.");
            }

            var inv = _context.Inventories.Find(inventoryId);
            if (inv == null) return NotFound();

            if (!ModelState.IsValid)
            {
                return View("AdjustStock", inv);
            }

            inv.InventoryTotalQuantity = newQuantity;
            inv.InventoryLastUpdated = DateTime.Now;

            _context.SaveChanges();
            return RedirectToAction(nameof(StockIndex));
        }

        // =========================
        // BATCH ADJUST (GET)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BatchAdjust(List<int> selectedInventoryIds)
        {
            if (selectedInventoryIds == null || !selectedInventoryIds.Any())
                return RedirectToAction(nameof(StockIndex));

            var inventories = _context.Inventories
                .Where(i => selectedInventoryIds.Contains(i.InventoryID))
                .ToList();

            return View("BatchAdjustStock", inventories);
        }

        // =========================
        // BATCH ADJUST (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmBatchAdjust(
            List<int> inventoryIds,
            string adjustmentType,
            int quantity,
            string reason)
        {
            if (quantity <= 0)
            {
                ModelState.AddModelError("", "Quantity must be greater than zero.");
            }

            var inventories = _context.Inventories
                .Where(i => inventoryIds.Contains(i.InventoryID))
                .ToList();

            if (!ModelState.IsValid)
            {
                return View("BatchAdjustStock", inventories);
            }

            foreach (var inv in inventories)
            {
                if (adjustmentType == "IN")
                {
                    inv.InventoryTotalQuantity += quantity;
                }
                else // OUT
                {
                    if (inv.InventoryTotalQuantity < quantity)
                        continue;

                    inv.InventoryTotalQuantity -= quantity;
                }

                inv.InventoryLastUpdated = DateTime.Now;
            }

            _context.SaveChanges();
            return RedirectToAction(nameof(StockIndex));
        }
    }
}

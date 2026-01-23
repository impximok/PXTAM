using Invexaaa.Data;
using Invexaaa.Models.Invexa;
using Invexaaa.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Invexaaa.Controllers
{
    public class StockController : Controller
    {
        private readonly InvexaDbContext _context;

        public StockController(InvexaDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // MANAGE STOCK
        // =====================================================
        public IActionResult StockIndex()
        {
            var list =
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
                    ItemImageUrl = item.ItemImageUrl,
                    StockStatus =
                        inv.InventoryTotalQuantity == 0 ? "Out of Stock" :
                        inv.InventoryTotalQuantity <= item.ItemReorderLevel ? "Low Stock" :
                        "In Stock",
                    LastUpdated = inv.InventoryLastUpdated
                };

            return View(list.ToList());
        }

        // =====================================================
        // ADD STOCK (GET – SINGLE)
        // =====================================================
        public IActionResult AddStockBatch(int inventoryId)
        {
            return View(new AddStockBatchViewModel
            {
                InventoryIds = new List<int> { inventoryId }
            });
        }

        // =====================================================
        // ADD STOCK (GET – BULK)
        // =====================================================
        public IActionResult AddStockBatchBulk(string inventoryIds)
        {
            return View("AddStockBatch", new AddStockBatchViewModel
            {
                InventoryIds = inventoryIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToList()
            });
        }

        // =====================================================
        // ADD STOCK (POST)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddStockBatch(AddStockBatchViewModel vm)
        {
            if (vm.InventoryIds == null || !vm.InventoryIds.Any())
                ModelState.AddModelError("", "No inventory selected.");

            if (vm.Quantity <= 0)
                ModelState.AddModelError("Quantity", "Quantity must be greater than 0.");

            if (!vm.ExpiryDate.HasValue || vm.ExpiryDate.Value <= DateTime.Today)
                ModelState.AddModelError("ExpiryDate", "Expiry date must be a future date.");

            if (!ModelState.IsValid)
                return View(vm);

            foreach (var inventoryId in vm.InventoryIds)
            {
                var inv = _context.Inventories.Find(inventoryId);
                if (inv == null) continue;

                // ✅ SYSTEM-GENERATED BATCH NUMBER
                var batchNo = $"BATCH-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid():N}".Substring(0, 20);

                _context.StockBatches.Add(new StockBatch
                {
                    ItemID = inv.ItemID,
                    BatchNumber = batchNo,
                    BatchQuantity = vm.Quantity,
                    BatchExpiryDate = vm.ExpiryDate.Value,
                    BatchReceivedDate = DateTime.Now,
                    BatchStatus = "Safe"
                });

                inv.InventoryTotalQuantity += vm.Quantity;
                inv.InventoryLastUpdated = DateTime.Now;

                _context.StockTransactions.Add(new StockTransaction
                {
                    ItemID = inv.ItemID,
                    TransactionType = "IN",
                    TransactionQuantity = vm.Quantity,
                    TransactionRemark = $"Stock received ({batchNo})"
                });
            }

            _context.SaveChanges();

            // ✅ SUCCESS MESSAGE
            TempData["SuccessMessage"] =
                vm.InventoryIds.Count > 1
                    ? "Stock successfully added to selected items."
                    : "Stock successfully added.";

            return RedirectToAction(nameof(StockIndex));
        }

        // =====================================================
        // ADJUST STOCK (GET)
        // =====================================================
        public IActionResult AdjustStock(int inventoryId)
        {
            var vm =
                (from inv in _context.Inventories
                 join item in _context.Items on inv.ItemID equals item.ItemID
                 where inv.InventoryID == inventoryId
                 select new AddStockBatchViewModel
                 {
                     InventoryIds = new List<int> { inventoryId },

                     CurrentQuantity = inv.InventoryTotalQuantity,
                     Quantity = inv.InventoryTotalQuantity,

                     ItemName = item.ItemName,
                     ItemUnitOfMeasure = item.ItemUnitOfMeasure,
                     ItemReorderLevel = item.ItemReorderLevel,
                     SafetyStock = item.SafetyStock,
                     ReorderPoint = item.ReorderPoint
                 }).FirstOrDefault();

            if (vm == null)
                return NotFound();

            return View(vm);
        }

        // =====================================================
        // ADJUST STOCK (POST)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AdjustStock(AddStockBatchViewModel vm)
        {
            if (vm.InventoryIds == null || !vm.InventoryIds.Any())
                ModelState.AddModelError("", "No inventory selected.");

            var newQuantity = vm.CurrentQuantity + vm.AdjustBy;

            if (newQuantity < 0)
                ModelState.AddModelError("AdjustBy", "Resulting quantity cannot be negative.");

            if (string.IsNullOrWhiteSpace(vm.AdjustmentNote))
                ModelState.AddModelError("AdjustmentNote", "Adjustment note is required.");

            if (!ModelState.IsValid)
            {
                vm.Quantity = vm.CurrentQuantity;
                return View(vm);
            }

            var inventoryId = vm.InventoryIds.First();
            var inv = _context.Inventories.Find(inventoryId);
            if (inv == null) return NotFound();

            // ✅ CREATE ADJUSTMENT (ID auto-generated by DB)
            var adjustment = new StockAdjustment
            {
                AdjustmentDate = DateTime.Now,
                AdjustmentReason = vm.AdjustmentNote,
                AdjustmentStatus = "Approved",
                CreatedByUserID = 1 // TODO: replace with logged-in user
            };

            _context.StockAdjustments.Add(adjustment);
            _context.SaveChanges();

            var diff = newQuantity - inv.InventoryTotalQuantity;

            _context.StockAdjustmentDetails.Add(new StockAdjustmentDetail
            {
                AdjustmentID = adjustment.AdjustmentID,
                ItemID = inv.ItemID,
                QuantityBefore = inv.InventoryTotalQuantity,
                QuantityAfter = newQuantity,
                QuantityDifference = diff
            });

            inv.InventoryTotalQuantity = newQuantity;
            inv.InventoryLastUpdated = DateTime.Now;

            _context.StockTransactions.Add(new StockTransaction
            {
                ItemID = inv.ItemID,
                TransactionType = "ADJUST",
                TransactionQuantity = diff,
                TransactionRemark = $"Manual adjustment (ADJ-{adjustment.AdjustmentID})"
            });

            _context.SaveChanges();

            // ✅ SUCCESS MESSAGE
            TempData["SuccessMessage"] = "Stock adjustment recorded successfully.";

            return RedirectToAction(nameof(StockIndex));
        }
    }
}

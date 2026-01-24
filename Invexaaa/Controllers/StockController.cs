using Invexaaa.Data;
using Invexaaa.Models.Invexa;
using Invexaaa.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Invexaaa.Controllers
{
    public class StockController : Controller
    {
        private readonly InvexaDbContext _context;

        public StockController(InvexaDbContext context)
        {
            _context = context;
        }

        // ============================
        // STOCK OVERVIEW
        // ============================
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
                    StockStatus =
                        inv.InventoryTotalQuantity == 0 ? "Out of Stock" :
                        inv.InventoryTotalQuantity <= item.ItemReorderLevel ? "Low Stock" :
                        "In Stock",
                    LastUpdated = inv.InventoryLastUpdated
                };

            return View(list.ToList());
        }

        // ============================
        // ADD STOCK (GET)
        // ============================
        [HttpGet]
        public IActionResult AddStockBatch(int inventoryId)
        {
            return View(new AddStockBatchViewModel
            {
                InventoryIds = new() { inventoryId }
            });
        }

        [HttpGet]
        public IActionResult AddStockBatchBulk(string inventoryIds)
        {
            if (string.IsNullOrWhiteSpace(inventoryIds))
                return RedirectToAction(nameof(StockIndex));

            return View("AddStockBatch", new AddStockBatchViewModel
            {
                InventoryIds = inventoryIds.Split(',').Select(int.Parse).ToList()
            });
        }

        // ============================
        // ADD STOCK (POST)
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddStockBatch(AddStockBatchViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            foreach (var inventoryId in vm.InventoryIds)
            {
                var inv = _context.Inventories.Find(inventoryId);
                if (inv == null) continue;

                var batch = new StockBatch
                {
                    ItemID = inv.ItemID,
                    BatchNumber = $"BATCH-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid():N}".Substring(0, 20),
                    BatchQuantity = vm.Quantity,
                    BatchExpiryDate = vm.ExpiryDate!.Value,
                    BatchReceivedDate = DateTime.Now
                };

                _context.StockBatches.Add(batch);
                _context.SaveChanges();

                inv.InventoryTotalQuantity += vm.Quantity;
                inv.InventoryLastUpdated = DateTime.Now;

                _context.StockTransactions.Add(new StockTransaction
                {
                    UserID = userId,
                    ItemID = inv.ItemID,
                    BatchID = batch.BatchID,
                    TransactionType = "IN",
                    TransactionQuantity = vm.Quantity,
                    TransactionRemark = $"Stock received ({batch.BatchNumber})"
                });
            }

            _context.SaveChanges();
            return RedirectToAction(nameof(StockIndex));
        }

        // ============================
        // ADJUST STOCK (GET)
        // ============================
        [HttpGet]
        public IActionResult AdjustStock(int inventoryId)
        {
            var vm =
                (from inv in _context.Inventories
                 join item in _context.Items on inv.ItemID equals item.ItemID
                 where inv.InventoryID == inventoryId
                 select new AdjustStockViewModel
                 {
                     InventoryIds = new() { inventoryId },
                     CurrentQuantity = inv.InventoryTotalQuantity,
                     ItemName = item.ItemName,
                     ItemUnitOfMeasure = item.ItemUnitOfMeasure,
                     ItemReorderLevel = item.ItemReorderLevel,
                     SafetyStock = item.SafetyStock,
                     ReorderPoint = item.ReorderPoint
                 }).FirstOrDefault();

            return vm == null ? NotFound() : View(vm);
        }

        // ============================
        // ADJUST STOCK (POST)
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AdjustStock(AdjustStockViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var inv = _context.Inventories.Find(vm.InventoryIds.First())!;
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var newQty = inv.InventoryTotalQuantity + vm.AdjustBy;

            var adjustment = new StockAdjustment
            {
                AdjustmentDate = DateTime.Now,
                AdjustmentReason = vm.AdjustmentNote,
                AdjustmentStatus = "Approved",
                CreatedByUserID = userId
            };

            _context.StockAdjustments.Add(adjustment);
            _context.SaveChanges();

            inv.InventoryTotalQuantity = newQty;
            inv.InventoryLastUpdated = DateTime.Now;

            _context.StockAdjustmentDetails.Add(new StockAdjustmentDetail
            {
                AdjustmentID = adjustment.AdjustmentID,
                ItemID = inv.ItemID,
                QuantityBefore = inv.InventoryTotalQuantity,
                QuantityAfter = newQty,
                QuantityDifference = vm.AdjustBy
            });

            _context.SaveChanges();
            return RedirectToAction(nameof(StockIndex));
        }

        // =====================================================
        // ADJUSTMENT HISTORY
        // =====================================================
        public IActionResult StockAdjustmentHistory()
        {
            var history =
                from d in _context.StockAdjustmentDetails
                join a in _context.StockAdjustments on d.AdjustmentID equals a.AdjustmentID
                join i in _context.Items on d.ItemID equals i.ItemID
                join t in _context.StockTransactions on d.ItemID equals t.ItemID
                join b in _context.StockBatches on t.BatchID equals b.BatchID into bb
                from b in bb.DefaultIfEmpty()
                orderby a.AdjustmentDate descending
                select new StockAdjustmentHistoryViewModel
                {
                    AdjustmentDate = a.AdjustmentDate,
                    ItemName = i.ItemName,
                    BatchNumber = b != null ? b.BatchNumber : "-",
                    QuantityBefore = d.QuantityBefore,
                    QuantityAfter = d.QuantityAfter,
                    QuantityDifference = d.QuantityDifference,
                    AdjustmentStatus = a.AdjustmentStatus,
                    AdjustmentReason = a.AdjustmentReason
                };

            return View("StockAdjustmentHistory", history.ToList());
        }

        // =====================================================
        // STOCK BATCH LIST
        // =====================================================
        public IActionResult StockBatchList()
        {
            var today = DateTime.Today;

            var list =
                from b in _context.StockBatches
                join i in _context.Items on b.ItemID equals i.ItemID
                select new StockBatchListViewModel
                {
                    ItemName = i.ItemName,
                    BatchNumber = b.BatchNumber,
                    BatchQuantity = b.BatchQuantity,
                    ExpiryDate = b.BatchExpiryDate,
                    BatchStatus =
                        b.BatchExpiryDate < today ? "Expired" :
                        b.BatchExpiryDate <= today.AddDays(30) ? "Near Expiry" :
                        "Safe"
                };

            return View(list.OrderBy(x => x.ExpiryDate).ToList());
        }
    }
}

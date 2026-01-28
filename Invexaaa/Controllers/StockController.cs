using Invexaaa.Data;
using Invexaaa.Models.Invexa;
using Invexaaa.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        // =====================================================
        // GLOBAL GUARD: Block inactive items from stock actions
        // =====================================================
        private bool IsItemInactive(int itemId)
        {
            return _context.Items.Any(i =>
                i.ItemID == itemId &&
                i.ItemStatus != "Active");
        }

        private List<AddStockPreviewItem> BuildAddStockPreview(List<int> ids)
        {
            if (ids == null || ids.Count == 0) return new List<AddStockPreviewItem>();

            return (from inv in _context.Inventories
                    join item in _context.Items on inv.ItemID equals item.ItemID
                    where ids.Contains(inv.InventoryID)
                    select new AddStockPreviewItem
                    {
                        InventoryID = inv.InventoryID,
                        ItemName = item.ItemName
                    }).ToList();
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
    inv.InventoryTotalQuantity <= item.ReorderPoint ? "Reorder" :
    inv.InventoryTotalQuantity <= item.ItemReorderLevel ? "Low Stock" :
    "In Stock",

                    LastUpdated = inv.InventoryLastUpdated,
                    ItemStatus = item.ItemStatus
                };

            return View(list.ToList());
        }

        [HttpGet]
        public IActionResult AdjustStockByBatch(int inventoryId)
        {
            var inventoryData =
                (from inv in _context.Inventories
                 join item in _context.Items on inv.ItemID equals item.ItemID
                 where inv.InventoryID == inventoryId
                 select new
                 {
                     inv.InventoryID,
                     inv.ItemID,
                     inv.InventoryTotalQuantity,
                     item.ItemName,
                     item.ItemUnitOfMeasure
                 }).FirstOrDefault();

            if (inventoryData == null)
                return NotFound();

            var batches = _context.StockBatches
                .Where(b => b.ItemID == inventoryData.ItemID)
                .OrderBy(b => b.BatchExpiryDate)
                .Select(b => new AdjustStockBatchRowViewModel
                {
                    BatchID = b.BatchID,
                    BatchNumber = b.BatchNumber,
                    BatchExpiryDate = b.BatchExpiryDate,
                    AvailableQuantity = b.BatchQuantity
                })
                .ToList();

            var vm = new AdjustStockByBatchViewModel
            {
                InventoryID = inventoryData.InventoryID,
                ItemID = inventoryData.ItemID,
                ItemName = inventoryData.ItemName,
                ItemUnitOfMeasure = inventoryData.ItemUnitOfMeasure,
                CurrentInventoryQuantity = inventoryData.InventoryTotalQuantity,
                Batches = batches
            };

            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AdjustStockByBatch(AdjustStockByBatchViewModel vm)
        {

            // 🔒 BLOCK inactive items
            if (IsItemInactive(vm.ItemID))
            {
                TempData["Error"] = "Inactive items cannot be adjusted.";
                return RedirectToAction("ItemDetail", "Item", new { id = vm.ItemID });
            }

            // ✅ VALIDATION GUARD (MISSING)
            if (!ModelState.IsValid)
            {
                return View(vm);
            }


            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            using var tx = _context.Database.BeginTransaction();

            try
            {
                var inventory = _context.Inventories
                    .First(i => i.InventoryID == vm.InventoryID);

                var inventoryQtyBefore = inventory.InventoryTotalQuantity;
                int netInventoryChange = 0;

                var adjustment = new StockAdjustment
                {
                    AdjustmentDate = DateTime.Now,
                    AdjustmentReason = vm.AdjustmentReason,
                    AdjustmentStatus = "Approved",
                    CreatedByUserID = userId
                };

                _context.StockAdjustments.Add(adjustment);
                _context.SaveChanges();

                foreach (var row in vm.Batches)
                {
                    if (row.AdjustQuantity == 0)
                        continue;

                    var batch = _context.StockBatches
                        .First(b => b.BatchID == row.BatchID);

                    // ❌ Prevent negative batch quantity
                    if (batch.BatchQuantity + row.AdjustQuantity < 0)
                    {
                        ModelState.AddModelError("",
                            $"Batch {batch.BatchNumber} cannot go below zero.");
                        return View(vm);
                    }

                    var batchQtyBefore = batch.BatchQuantity;

                    // ✅ Apply batch change
                    batch.BatchQuantity += row.AdjustQuantity;
                    netInventoryChange += row.AdjustQuantity;

                    // ✅ Stock transaction
                    _context.StockTransactions.Add(new StockTransaction
                    {
                        UserID = userId,
                        ItemID = vm.ItemID,
                        BatchID = batch.BatchID,
                        TransactionType = row.AdjustQuantity > 0 ? "IN" : "OUT",
                        TransactionQuantity = Math.Abs(row.AdjustQuantity),
                        TransactionRemark = vm.AdjustmentReason
                    });

                    // ✅ Per-batch adjustment detail (AUDIT GOLD)
                    _context.StockAdjustmentDetails.Add(new StockAdjustmentDetail
                    {
                        AdjustmentID = adjustment.AdjustmentID,
                        ItemID = vm.ItemID,
                        BatchID = batch.BatchID,
                        QuantityBefore = batchQtyBefore,
                        QuantityAfter = batch.BatchQuantity,
                        QuantityDifference = row.AdjustQuantity
                    });
                }

                if (netInventoryChange == 0)
                {
                    ModelState.AddModelError("", "No adjustments were entered.");
                    return View(vm);
                }

                // ✅ Update inventory total
                inventory.InventoryTotalQuantity += netInventoryChange;
                inventory.InventoryLastUpdated = DateTime.Now;

                _context.SaveChanges();
                tx.Commit();

                return RedirectToAction(nameof(StockIndex));
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // ============================
        // ADD STOCK (GET)
        // ============================
        [HttpGet]
        public IActionResult AddStockBatch(string inventoryIds)
        {
            if (string.IsNullOrWhiteSpace(inventoryIds))
                return RedirectToAction(nameof(StockIndex));

            var ids = inventoryIds.Split(',').Select(int.Parse).ToList();

            var previewItems =
                (from inv in _context.Inventories
                 join item in _context.Items on inv.ItemID equals item.ItemID
                 where ids.Contains(inv.InventoryID)
                 select new AddStockPreviewItem
                 {
                     InventoryID = inv.InventoryID,
                     ItemName = item.ItemName
                 }).ToList();

            return View(new AddStockBatchViewModel
            {
                InventoryIds = ids,
                PreviewItems = previewItems
            });
        }



        // ============================
        // ADD STOCK (POST)
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStockBatch(AddStockBatchViewModel vm)
        {
            vm.PreviewItems = BuildAddStockPreview(vm.InventoryIds);

            if (!ModelState.IsValid)
                return View(vm);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                int successCount = 0;
                int inactiveCount = 0;

                foreach (var inventoryId in vm.InventoryIds)
                {
                    var invData =
     await (from inventory in _context.Inventories
            join item in _context.Items on inventory.ItemID equals item.ItemID
            where inventory.InventoryID == inventoryId
            select new
            {
                Inventory = inventory,
                ItemName = item.ItemName
            }).FirstOrDefaultAsync();


                    if (invData == null) continue;

                    var inv = invData.Inventory;
                    var itemName = invData.ItemName;

                    // 🔒 BLOCK inactive items
                    if (IsItemInactive(inv.ItemID))
                    {
                        inactiveCount++;
                        continue;
                    }

                    var batch = new StockBatch
                    {
                        ItemID = inv.ItemID,
                        BatchNumber = $"BATCH-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid():N}".Substring(0, 20),
                        BatchQuantity = vm.Quantity,
                        BatchExpiryDate = vm.ExpiryDate!.Value
                    };


                    _context.StockBatches.Add(batch);

                    inv.InventoryTotalQuantity += vm.Quantity;
                    inv.InventoryLastUpdated = DateTime.Now;

                    _context.StockTransactions.Add(new StockTransaction
                    {
                        UserID = userId,
                        ItemID = inv.ItemID,
                        TransactionType = "IN",
                        TransactionQuantity = vm.Quantity,
                        TransactionRemark = "Stock received"
                    });

                    // ✅ AFTER-SAVE SUMMARY
                    vm.SummaryRows.Add(new AddStockBatchSummaryRow
                    {
                        ItemName = itemName,
                        QuantityAdded = vm.Quantity,
                        ExpiryDate = vm.ExpiryDate.Value
                    });
                    successCount++;
                }

                if (successCount == 0)
                {
                    ModelState.AddModelError("", inactiveCount > 0
                        ? "All selected items are inactive. No stock was added."
                        : "No stock was added. Please retry.");

                    await transaction.RollbackAsync();
                    return View(vm);
                }

                if (inactiveCount > 0)
                {
                    TempData["Error"] = $"{inactiveCount} inactive item(s) were skipped.";
                }


                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                vm.ShowSummary = true;

                return View(vm);
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Stock was updated by another user. Please retry.");
                return View(vm);
            }
        }

        [HttpGet]
        public IActionResult MinusStockBatchBulk(string inventoryIds)
        {
            if (string.IsNullOrWhiteSpace(inventoryIds))
                return RedirectToAction(nameof(StockIndex));

            var ids = inventoryIds.Split(',').Select(int.Parse).ToList();

            var preview =
                from inv in _context.Inventories
                join item in _context.Items on inv.ItemID equals item.ItemID
                where ids.Contains(inv.InventoryID)
                && item.ItemStatus == "Active"
                select new BulkMinusPreviewRow
                {
                    InventoryID = inv.InventoryID,
                    ItemName = item.ItemName,
                    AvailableQuantity = inv.InventoryTotalQuantity
                };

            return View(new BulkMinusStockViewModel
            {
                InventoryIds = ids,
                PreviewItems = preview.ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MinusStockBatchBulk(BulkMinusStockViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            using var tx = _context.Database.BeginTransaction();

            try
            {
                foreach (var inventoryId in vm.InventoryIds)
                {
                    var inv =
    _context.Inventories
        .First(i => i.InventoryID == inventoryId);


                    if (IsItemInactive(inv.ItemID))
                        continue;

                    if (vm.QuantityToDeduct > inv.InventoryTotalQuantity)
                        continue;

                    int remaining = vm.QuantityToDeduct;

                    var batches = _context.StockBatches
                        .Where(b => b.ItemID == inv.ItemID && b.BatchQuantity > 0)
                        .OrderBy(b => b.BatchExpiryDate)
                        .ToList();

                    foreach (var batch in batches)
                    {
                        if (remaining <= 0) break;

                        var deduct = Math.Min(batch.BatchQuantity, remaining);
                        batch.BatchQuantity -= deduct;
                        remaining -= deduct;

                        _context.StockTransactions.Add(new StockTransaction
                        {
                            UserID = userId,
                            ItemID = inv.ItemID,
                            BatchID = batch.BatchID,
                            TransactionType = "OUT",
                            TransactionQuantity = deduct,
                            TransactionRemark = vm.Reason
                        });
                    }

                    inv.InventoryTotalQuantity -= vm.QuantityToDeduct;
                    inv.InventoryLastUpdated = DateTime.Now;
                }

                _context.SaveChanges();
                tx.Commit();

                return RedirectToAction(nameof(StockIndex));
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }


        // =====================================================
        // ADJUSTMENT HISTORY
        // =====================================================
        public IActionResult StockAdjustmentHistory()
        {
            var history =
                from d in _context.StockAdjustmentDetails
                join a in _context.StockAdjustments
                    on d.AdjustmentID equals a.AdjustmentID
                join i in _context.Items
                    on d.ItemID equals i.ItemID
                join b in _context.StockBatches
                    on d.BatchID equals b.BatchID
                orderby a.AdjustmentDate descending
                select new StockAdjustmentHistoryViewModel
                {
                    AdjustmentDate = a.AdjustmentDate,
                    ItemName = i.ItemName,
                    BatchNumber = b.BatchNumber,
                    QuantityBefore = d.QuantityBefore,
                    QuantityAfter = d.QuantityAfter,
                    QuantityDifference = d.QuantityDifference,
                    AdjustmentStatus = a.AdjustmentStatus,
                    AdjustmentReason = a.AdjustmentReason,
                    ItemStatus = i.ItemStatus
                };

            return View("StockAdjustmentHistory", history.ToList());
        }

        public IActionResult StockTransactionHistory()
        {
            var history =
                from t in _context.StockTransactions
                join i in _context.Items on t.ItemID equals i.ItemID
                join u in _context.Users on t.UserID equals u.UserID
                join b in _context.StockBatches on t.BatchID equals b.BatchID into bj
                from batch in bj.DefaultIfEmpty()
                orderby t.TransactionDate descending
                select new StockTransactionHistoryViewModel
                {
                    TransactionDate = t.TransactionDate,
                    ItemName = i.ItemName,
                    BatchNumber = batch != null ? batch.BatchNumber : "-",
                    TransactionType = t.TransactionType,
                    TransactionQuantity = t.TransactionQuantity,
                    TransactionRemark = t.TransactionRemark,
                    UserName = u.UserFullName


                };

            return View(history.ToList());
        }


        // ==============================
        // EDIT EXPIRY (GET)
        // ==============================
        public IActionResult EditExpiry(int batchId)
        {
            var batch = _context.StockBatches.FirstOrDefault(b => b.BatchID == batchId);
            if (batch == null)
                return NotFound();

            var item = _context.Items.FirstOrDefault(i => i.ItemID == batch.ItemID);
            if (item == null || item.ItemStatus != "Active")
            {
                TempData["Error"] = "Inactive items cannot be modified.";
                return RedirectToAction("ItemDetail", "Item", new { id = batch.ItemID });
            }

            ViewBag.ItemName = item.ItemName;
            return View(batch);
        }

        // ==============================
        // EDIT EXPIRY (POST)
        // ==============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditExpiry(int BatchID, DateTime BatchExpiryDate, string Reason)
        {
            if (string.IsNullOrWhiteSpace(Reason))
            {
                ModelState.AddModelError("", "Reason is required.");
                return View();
            }

            var batch = _context.StockBatches.FirstOrDefault(b => b.BatchID == BatchID);
            if (batch == null)
                return NotFound();

            var item = _context.Items.FirstOrDefault(i => i.ItemID == batch.ItemID);
            if (item == null || item.ItemStatus != "Active")
            {
                TempData["Error"] = "Inactive items cannot be modified.";
                return RedirectToAction("ItemDetail", "Item", new { id = batch.ItemID });
            }

            batch.BatchExpiryDate = BatchExpiryDate;
            _context.SaveChanges();

            return RedirectToAction("ExpiryTrackingIndex", "ExpiryTracking");
        }

    }
}

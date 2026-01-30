using DocumentFormat.OpenXml.Spreadsheet;
using Invexaaa.Data;
using Invexaaa.Models.Invexa;
using Invexaaa.Models.Invexa.Enums;
using Invexaaa.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq;
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

            vm.AvailableUnits = _context.ItemUnitConversions
    .Where(u => u.ItemID == vm.ItemID)
    .OrderByDescending(u => u.IsBaseUnit)
    .ToList();


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

            // 🔁 Reload units + batches on validation failure
            if (!ModelState.IsValid)
            {
                vm.Batches = _context.StockBatches
                    .Where(b => b.ItemID == vm.ItemID)
                    .OrderBy(b => b.BatchExpiryDate)
                    .Select(b => new AdjustStockBatchRowViewModel
                    {
                        BatchID = b.BatchID,
                        BatchNumber = b.BatchNumber,
                        BatchExpiryDate = b.BatchExpiryDate,
                        AvailableQuantity = b.BatchQuantity
                    })
                    .ToList();

                vm.AvailableUnits = _context.ItemUnitConversions
                    .Where(u => u.ItemID == vm.ItemID)
                    .OrderByDescending(u => u.IsBaseUnit)
                    .ToList();

                return View(vm);
            }

            if (string.IsNullOrWhiteSpace(vm.AdjustmentReason))
            {
                ModelState.AddModelError("", "Adjustment reason is required.");
                return View(vm);
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            using var tx = _context.Database.BeginTransaction();

            try
            {
                var inventory = _context.Inventories.First(i => i.InventoryID == vm.InventoryID);
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
                    if (row.InputQuantity == 0)
                        continue;

                    // 🔒 Unit required
                    if (row.UnitConversionID <= 0)
                    {
                        ModelState.AddModelError("", $"Please select a unit for batch {row.BatchNumber}.");

                        vm.Batches = _context.StockBatches
                            .Where(b => b.ItemID == vm.ItemID)
                            .OrderBy(b => b.BatchExpiryDate)
                            .Select(b => new AdjustStockBatchRowViewModel
                            {
                                BatchID = b.BatchID,
                                BatchNumber = b.BatchNumber,
                                BatchExpiryDate = b.BatchExpiryDate,
                                AvailableQuantity = b.BatchQuantity
                            })
                            .ToList();

                        vm.AvailableUnits = _context.ItemUnitConversions
                            .Where(u => u.ItemID == vm.ItemID)
                            .OrderByDescending(u => u.IsBaseUnit)
                            .ToList();

                        return View(vm);
                    }

                    var batch = _context.StockBatches.First(b => b.BatchID == row.BatchID);
                    var before = batch.BatchQuantity;

                    int baseQty = ConvertToBaseUnit(
                        vm.ItemID,
                        row.InputQuantity,
                        row.UnitConversionID
                    );

                    row.BaseQuantity = baseQty;

                    // 🔒 Prevent negative batch
                    if (batch.BatchQuantity + baseQty < 0)
                    {
                        ModelState.AddModelError("", $"Batch {batch.BatchNumber} cannot go below zero.");

                        vm.Batches = _context.StockBatches
                            .Where(b => b.ItemID == vm.ItemID)
                            .OrderBy(b => b.BatchExpiryDate)
                            .Select(b => new AdjustStockBatchRowViewModel
                            {
                                BatchID = b.BatchID,
                                BatchNumber = b.BatchNumber,
                                BatchExpiryDate = b.BatchExpiryDate,
                                AvailableQuantity = b.BatchQuantity
                            })
                            .ToList();

                        vm.AvailableUnits = _context.ItemUnitConversions
                            .Where(u => u.ItemID == vm.ItemID)
                            .OrderByDescending(u => u.IsBaseUnit)
                            .ToList();

                        return View(vm);
                    }

                    batch.BatchQuantity += baseQty;
                    netInventoryChange += baseQty;

                    var item = _context.Items.First(i => i.ItemID == vm.ItemID);

                    _context.StockTransactions.Add(new StockTransaction
                    {
                        UserID = userId,
                        ItemID = vm.ItemID,
                        BatchID = batch.BatchID,
                        TransactionType = baseQty > 0 ? "IN" : "OUT",
                        TransactionQuantity = Math.Abs(baseQty),
                        CostingMethodUsed = item.CostingMethod,
                        TransactionRemark = vm.AdjustmentReason
                    });

                    _context.StockAdjustmentDetails.Add(new StockAdjustmentDetail
                    {
                        AdjustmentID = adjustment.AdjustmentID,
                        ItemID = vm.ItemID,
                        BatchID = batch.BatchID,
                        QuantityBefore = before,
                        QuantityAfter = batch.BatchQuantity,
                        QuantityDifference = baseQty
                    });
                }

                if (netInventoryChange == 0)
                {
                    ModelState.AddModelError("", "No adjustments were entered.");
                    return View(vm);
                }

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

            // 🔥 GET ITEM ID FROM FIRST INVENTORY
            var firstInventoryId = ids.First();

            var itemId = _context.Inventories
                .Where(i => i.InventoryID == firstInventoryId)
                .Select(i => i.ItemID)
                .First();

            // 🔥 LOAD UNIT CONVERSIONS
            var units = _context.ItemUnitConversions
                .Where(u => u.ItemID == itemId)
                .OrderByDescending(u => u.IsBaseUnit)
                .ToList();

            return View(new AddStockBatchViewModel
            {
                InventoryIds = ids,
                PreviewItems = previewItems,
                AvailableUnits = units
            });
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStockBatch(AddStockBatchViewModel vm)
        {
            vm.PreviewItems = BuildAddStockPreview(vm.InventoryIds);
            // 🔁 RELOAD UNIT CONVERSIONS (IMPORTANT)
            var firstInventoryId = vm.InventoryIds.First();

            var itemId = _context.Inventories
                .Where(i => i.InventoryID == firstInventoryId)
                .Select(i => i.ItemID)
                .First();

            vm.AvailableUnits = _context.ItemUnitConversions
                .Where(u => u.ItemID == itemId)
                .OrderByDescending(u => u.IsBaseUnit)
                .ThenBy(u => u.UnitName)
                .ToList();

            // 🔒 Validate ALL inventory IDs before processing (no partial success)
            foreach (var inventoryId in vm.InventoryIds)
            {
                var invData =
                    await (from inventory in _context.Inventories
                           join itm in _context.Items on inventory.ItemID equals itm.ItemID
                           where inventory.InventoryID == inventoryId
                           select new { inventory, itm })
                    .FirstOrDefaultAsync();

                if (invData == null || invData.itm.ItemStatus != "Active")
                {
                    ModelState.AddModelError(
                        "",
                        "One or more selected items are inactive or no longer exist."
                    );
                    return View(vm);
                }
            }


            if (!ModelState.IsValid)
                return View(vm);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var inventoryId in vm.InventoryIds)
                {
                    var invData =
    await (from inventory in _context.Inventories
           join itm in _context.Items on inventory.ItemID equals itm.ItemID
           where inventory.InventoryID == inventoryId
           select new
           {
               Inventory = inventory,
               Item = itm
           }).FirstOrDefaultAsync();


                    if (invData == null)
                        continue;

                    var inv = invData.Inventory;
                    var item = invData.Item;

                    // =========================
                    // 1️⃣ CONVERT INPUT → BASE UNIT (MUST BE FIRST)
                    // =========================
                    int inQty = ConvertToBaseUnit(
                        item.ItemID,
                        vm.InputQuantity,
                        vm.UnitConversionID
                    );

                    // =========================
                    // 2️⃣ CREATE STOCK BATCH (BASE UNIT ONLY)
                    // =========================
                    var batch = new StockBatch
                    {
                        ItemID = item.ItemID,
                        BatchNumber = $"BATCH-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid():N}"
                                        .Take(20)
                                        .Aggregate("", (a, c) => a + c),

                        BatchQuantity = inQty, // ✅ base unit only
                        BatchExpiryDate = vm.ExpiryDate!.Value,

                        TransactionUnitCost = vm.UnitCost,
                        SupplierNameSnapshot = vm.SupplierNameSnapshot,
                        LeadTimeDays = vm.LeadTimeDays
                    };

                    _context.StockBatches.Add(batch);

                    // =========================
                    // 2️⃣ COSTING LOGIC
                    // =========================
                    int oldQty = inv.InventoryTotalQuantity;
                    decimal oldAvg = inv.AverageUnitCost;

         
                    decimal inCost = vm.UnitCost;

                    int newQty = oldQty + inQty;

                    switch (item.CostingMethod)
                    {
                        case CostingMethod.WeightedAverage:
                            if (newQty > 0)
                            {
                                decimal oldValue = oldQty * oldAvg;
                                decimal newValue = inQty * inCost;

                                inv.AverageUnitCost = (oldValue + newValue) / newQty;
                            }
                            break;

                        case CostingMethod.Fixed:
                            // Fixed / standard cost NEVER changes on stock in
                            // Cost is defined at inventory level
                            break;


                        case CostingMethod.FIFO:
                            // Do NOT update inventory cost here
                            // FIFO valuation is batch-driven
                            break;

                    }


                    inv.InventoryTotalQuantity = newQty;

                    if (item.CostingMethod == CostingMethod.WeightedAverage)
                    {
                        inv.TotalStockValue = inv.AverageUnitCost * newQty;
                    }
                    else if (item.CostingMethod == CostingMethod.Fixed)
                    {
                        inv.TotalStockValue = inv.StandardUnitCost * newQty;
                    }

                    // FIFO: total value is derived from batches later (do NOT overwrite)

                    inv.InventoryLastUpdated = DateTime.Now;
                    inv.LastCostUpdated = DateTime.Now;


                    // =========================
                    // 3️⃣ STOCK TRANSACTION (IN)
                    // =========================
                    _context.StockTransactions.Add(new StockTransaction
                    {
                        UserID = userId,
                        ItemID = item.ItemID,
                        BatchID = batch.BatchID,
                        TransactionType = "IN",
                        TransactionQuantity = inQty,
                        UnitCost = inCost,
                        CostingMethodUsed = item.CostingMethod,
                        TransactionRemark = "Stock received"
                    });


                    // =========================
                    // SUMMARY
                    // =========================
                    vm.SummaryRows.Add(new AddStockBatchSummaryRow
                    {
                        ItemName = item.ItemName,
                        QuantityAdded = inQty,
                        ExpiryDate = vm.ExpiryDate.Value
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                vm.ShowSummary = true;
                return View(vm);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
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

            // 🔥 LOAD ITEM + UNITS (Multi-UOM)
            var firstInventoryId = ids.First();

            var itemId = _context.Inventories
                .Where(i => i.InventoryID == firstInventoryId)
                .Select(i => i.ItemID)
                .First();

            var units = _context.ItemUnitConversions
                .Where(u => u.ItemID == itemId)
                .OrderByDescending(u => u.IsBaseUnit)
                .ToList();

            return View(new BulkMinusStockViewModel
            {
                InventoryIds = ids,
                PreviewItems = preview.ToList(),
                AvailableUnits = units
            });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MinusStockBatchBulk(BulkMinusStockViewModel vm)
        {
            if (vm.InventoryIds == null || !vm.InventoryIds.Any())
            {
                return RedirectToAction(nameof(StockIndex));
            }

            vm.PreviewItems = BuildMinusPreview(vm.InventoryIds);

            var firstInventoryId = vm.InventoryIds.First();

            var itemId = _context.Inventories
                .Where(i => i.InventoryID == firstInventoryId)
                .Select(i => i.ItemID)
                .First();

            vm.AvailableUnits = _context.ItemUnitConversions
                .Where(u => u.ItemID == itemId)
                .OrderByDescending(u => u.IsBaseUnit)
                .ToList();

            if (!ModelState.IsValid)
                return View(vm);
            // 🔒 Unit must be selected
            if (vm.UnitConversionID <= 0)
            {
                ModelState.AddModelError(
                    nameof(vm.UnitConversionID),
                    "Please select a unit."
                );
                return View(vm);
            }


            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            using var tx = _context.Database.BeginTransaction();

            try
            {
                foreach (var inventoryId in vm.InventoryIds)
                {
                    var inv = _context.Inventories.First(i => i.InventoryID == inventoryId);

                    if (IsItemInactive(inv.ItemID))
                        continue;

                    int originalDeductQty = ConvertToBaseUnit(
     inv.ItemID,
     vm.InputQuantity,
     vm.UnitConversionID
 );
                    vm.BaseQuantity = originalDeductQty;

                    int remaining = originalDeductQty;


                    if (remaining > inv.InventoryTotalQuantity)
                    {
                        ModelState.AddModelError(
                            "",
                            $"Insufficient stock for item ID {inv.ItemID}."
                        );
                        return View(vm);
                    }



                    

                    // 🔥 FIFO: oldest batch first
                    var batches = _context.StockBatches
                        .Where(b => b.ItemID == inv.ItemID && b.BatchQuantity > 0)
                        .OrderBy(b => b.BatchExpiryDate)
                        .ThenBy(b => b.BatchID)
                        .ToList();

                    var item = _context.Items.First(i => i.ItemID == inv.ItemID);

                    decimal outUnitCost = item.CostingMethod switch
                    {
                        CostingMethod.WeightedAverage => inv.AverageUnitCost,
                        CostingMethod.Fixed => inv.StandardUnitCost,

                        _ => 0m // FIFO handled per batch
                    };

                    foreach (var batch in batches)
                    {
                        if (remaining <= 0)
                            break;

                        int deductQty = Math.Min(batch.BatchQuantity, remaining);

                        batch.BatchQuantity -= deductQty;
                        remaining -= deductQty;

                        // ✅ STOCK TRANSACTION (OUT)
                        _context.StockTransactions.Add(new StockTransaction
                        {
                            UserID = userId,
                            ItemID = inv.ItemID,
                            BatchID = batch.BatchID,
                            TransactionType = "OUT",
                            TransactionQuantity = deductQty,
                            UnitCost = item.CostingMethod == CostingMethod.FIFO
        ? batch.TransactionUnitCost
        : outUnitCost,
                            CostingMethodUsed = item.CostingMethod,
                            CustomerID = vm.CustomerID,
                            CustomerNameSnapshot = vm.CustomerNameSnapshot,
                            TransactionRemark = vm.Reason
                        });

                    }

                    inv.InventoryTotalQuantity -= originalDeductQty;


                    if (item.CostingMethod == CostingMethod.WeightedAverage)
                    {
                        inv.TotalStockValue = inv.InventoryTotalQuantity * inv.AverageUnitCost;
                    }
                    else if (item.CostingMethod == CostingMethod.Fixed)
                    {
                        inv.TotalStockValue = inv.InventoryTotalQuantity * inv.StandardUnitCost;
                    }

                    // FIFO: do NOT recalc here

                    inv.InventoryLastUpdated = DateTime.Now;

                }

                _context.SaveChanges();
                tx.Commit();

                vm.ShowSummary = true;
                return View(vm);
            }
            catch
            {
                if (tx.GetDbTransaction().Connection != null)
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
                    UnitCost = t.UnitCost,

                    SupplierName =
        t.TransactionType == "IN"
            ? batch.SupplierNameSnapshot
            : null,

                    CustomerName =
        t.TransactionType == "OUT"
            ? t.CustomerNameSnapshot
            : null,

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
        private List<BulkMinusPreviewRow> BuildMinusPreview(List<int> ids)
        {
            if (ids == null || ids.Count == 0) return new();

            return (from inv in _context.Inventories
                    join item in _context.Items on inv.ItemID equals item.ItemID
                    where ids.Contains(inv.InventoryID) && item.ItemStatus == "Active"
                    select new BulkMinusPreviewRow
                    {
                        InventoryID = inv.InventoryID,
                        ItemName = item.ItemName,
                        AvailableQuantity = inv.InventoryTotalQuantity
                    }).ToList();
        }
        // =====================================================
        // MULTI-UOM – CONVERT INPUT UNIT → BASE UNIT
        // =====================================================
        private int ConvertToBaseUnit(int itemId, int inputQty, int unitConversionId)
        {
            if (inputQty <= 0)
                throw new InvalidOperationException("Quantity must be greater than zero.");

            var conversion = _context.ItemUnitConversions
                .FirstOrDefault(u =>
                    u.ItemUnitConversionID == unitConversionId &&
                    u.ItemID == itemId);

            if (conversion == null)
                throw new InvalidOperationException("Invalid unit conversion selected.");

            checked
            {
                // Example:
                // 2 cartons × 24 = 48 pcs
                return inputQty * conversion.BaseUnitMultiplier;
            }
        }

    }
}

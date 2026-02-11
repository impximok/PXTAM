using Invexaaa.Data;
using Invexaaa.Models.Invexa;
using Invexaaa.Models.Invexa.Enums;
using Invexaaa.Models.Invexa.ViewModels;
using Invexaaa.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq;
using System.Security.Claims;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

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
                    CostingMethod =
    item.CostingMethod == CostingMethod.WeightedAverage
        ? "Weighted Average"
        : item.CostingMethod.ToString(),


                    StockStatus =
                        inv.InventoryTotalQuantity <= item.ReorderPoint ? "Reorder" :
                        inv.InventoryTotalQuantity <= item.ItemReorderLevel ? "Low Stock" :
                        "In Stock",

                    LastUpdated = inv.InventoryLastUpdated,
                    ItemStatus = item.ItemStatus
                };

            return View(list.ToList());
        }

        // ============================
        // ADJUST STOCK BY BATCH (GET)
        // ============================
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
                     item.ItemUnitOfMeasure,
                     item.CostingMethod
                 }).FirstOrDefault();

            if (inventoryData == null)
                return NotFound();

            // 🔒 Determine base unit once
            var baseUnitId = _context.ItemUnitConversions
                .Where(u => u.ItemID == inventoryData.ItemID && u.IsBaseUnit)
                .Select(u => (int?)u.ItemUnitConversionID)
                .FirstOrDefault();

            var batches = _context.StockBatches
                .Where(b => b.ItemID == inventoryData.ItemID)
                .OrderBy(b => b.BatchExpiryDate)
                .Select(b => new AdjustStockBatchRowViewModel
                {
                    BatchID = b.BatchID,
                    BatchNumber = b.BatchNumber,
                    BatchExpiryDate = b.BatchExpiryDate,
                    AvailableQuantity = b.BatchQuantity,

                    // 🔥 THIS IS THE KEY FIX
                    UnitConversionID =
                        (inventoryData.CostingMethod == CostingMethod.Fixed ||
                         inventoryData.CostingMethod == CostingMethod.WeightedAverage)
                            ? baseUnitId ?? 0
                            : 0
                })
                .ToList();


            var vm = new AdjustStockByBatchViewModel
            {
                InventoryID = inventoryData.InventoryID,
                ItemID = inventoryData.ItemID,
                ItemName = inventoryData.ItemName,
                ItemUnitOfMeasure = inventoryData.ItemUnitOfMeasure,
                CurrentInventoryQuantity = inventoryData.InventoryTotalQuantity,
                CostingMethod = inventoryData.CostingMethod,
                Batches = batches
            };

            vm.AvailableUnits = _context.ItemUnitConversions
                .Where(u => u.ItemID == vm.ItemID)
                .OrderByDescending(u => u.IsBaseUnit)
                .ToList();

            vm.Customers = _context.Customers
                .OrderBy(c => c.CustomerName)
                .ToList();

            return View(vm);
        }

        // ============================
        // ADJUST STOCK BY BATCH (POST)
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AdjustStockByBatch(AdjustStockByBatchViewModel vm)
        {
            void Fail(string code, string message)
            {
                ModelState.AddModelError("", $"[{code}] {message}");
            }

            // ✅ SOURCE OF TRUTH: always rehydrate costing method from DB
            vm.CostingMethod = _context.Items
                .Where(i => i.ItemID == vm.ItemID)
                .Select(i => i.CostingMethod)
                .First();
            ModelState.Remove(nameof(vm.CostingMethod));

            // 🔥 CLEAR INVALID UNIT MODELSTATE (disabled selects post empty)
            for (int i = 0; i < vm.Batches.Count; i++)
            {
                ModelState.Remove($"Batches[{i}].UnitConversionID");
            }


            var actualMethod = vm.CostingMethod;

            if (IsItemInactive(vm.ItemID))
            {
                TempData["Error"] = "Inactive items cannot be adjusted.";
                return RedirectToAction("ItemDetail", "Item", new { id = vm.ItemID });
            }

            

            if (string.IsNullOrWhiteSpace(vm.StockOutRemark))
            {
                ModelState.AddModelError("", "Adjustment reason is required.");
                return View(vm);
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            string? customerSnapshot = null;

            if (vm.CustomerID.HasValue)
            {
                customerSnapshot = _context.Customers
                    .Where(c => c.CustomerID == vm.CustomerID.Value)
                    .Select(c => c.CustomerName)
                    .FirstOrDefault();
            }
            else if (!string.IsNullOrWhiteSpace(vm.CustomerNameSnapshot))
            {
                customerSnapshot = vm.CustomerNameSnapshot.Trim();
            }

            using var tx = _context.Database.BeginTransaction();

            try
            {
                var inventory = _context.Inventories.First(i => i.InventoryID == vm.InventoryID);
                int netInventoryChange = 0;

                var adjustment = new StockAdjustment
                {
                    AdjustmentDate = DateTime.Now,
                    AdjustmentReason = vm.StockOutRemark,
                    AdjustmentStatus = "Approved",
                    CreatedByUserID = userId
                };

                _context.StockAdjustments.Add(adjustment);
                _context.SaveChanges();

                foreach (var row in vm.Batches)
                {
                    if (row.InputQuantity == 0)
                        continue;

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

                        vm.Customers = _context.Customers
                            .OrderBy(c => c.CustomerName)
                            .ToList();

                        return View(vm);
                    }

                    var item = _context.Items.First(i => i.ItemID == vm.ItemID);

                    if (item.CostingMethod == CostingMethod.Fixed ||
    item.CostingMethod == CostingMethod.WeightedAverage)
                    {
                        var baseUnit = _context.ItemUnitConversions
                            .FirstOrDefault(u => u.ItemID == vm.ItemID && u.IsBaseUnit);

                        if (baseUnit == null)
                        {
                            ModelState.AddModelError("", "Base unit is missing for this item. Please fix unit setup.");
                            RehydrateAdjustVm(vm);
                            return View(vm);
                        }

                        row.UnitConversionID = baseUnit.ItemUnitConversionID;

                        // 🔥 IMPORTANT: ModelState still has old (0) value
                        ModelState.Remove($"Batches[{vm.Batches.IndexOf(row)}].UnitConversionID");
                    }


                    bool unitValid = _context.ItemUnitConversions.Any(u =>
                        u.ItemUnitConversionID == row.UnitConversionID &&
                        u.ItemID == vm.ItemID);

                    if (!unitValid)
                    {
                        ModelState.AddModelError("", $"Invalid unit selected for batch {row.BatchNumber}.");
                        return View(vm);
                    }

                    var batch = _context.StockBatches.First(b => b.BatchID == row.BatchID);
                    var before = batch.BatchQuantity;

                    if (row.InputQuantity < 0)
                    {
                        var unit = _context.ItemUnitConversions
                            .First(u => u.ItemUnitConversionID == row.UnitConversionID);

                        if (!unit.IsBaseUnit)
                        {
                            ModelState.AddModelError("", $"Negative adjustment for batch {row.BatchNumber} must use base unit only.");
                            return View(vm);
                        }
                    }

                    int baseQty;
                    try
                    {
                        baseQty = ValidateAndConvertToBaseAllowNegative(
                            vm.ItemID,
                            row.InputQuantity,
                            row.UnitConversionID,
                            item.CostingMethod
                        );
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", ex.Message);
                        return View(vm);
                    }


                    row.BaseQuantity = baseQty;

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

                        vm.Customers = _context.Customers
                            .OrderBy(c => c.CustomerName)
                            .ToList();

                        return View(vm);
                    }

                    batch.BatchQuantity += baseQty;
                    netInventoryChange += baseQty;

                    _context.StockTransactions.Add(new StockTransaction
                    {
                        UserID = userId,
                        ItemID = vm.ItemID,
                        BatchID = batch.BatchID,
                        TransactionType = baseQty > 0 ? "IN" : "OUT",
                        TransactionQuantity = Math.Abs(baseQty),

                        UnitCost =
                            item.CostingMethod == CostingMethod.FIFO
                                ? batch.TransactionUnitCost
                                : item.CostingMethod == CostingMethod.Fixed
                                    ? inventory.StandardUnitCost
                                    : inventory.AverageUnitCost,

                        CostingMethodUsed = item.CostingMethod,
                        CustomerID = baseQty < 0 ? vm.CustomerID : null,
                        CustomerNameSnapshot = baseQty < 0 ? customerSnapshot : null,
                        TransactionRemark = baseQty < 0 ? vm.StockOutRemark : "Stock adjustment (increase)"
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
                    Fail("ADJ-01", "No adjustments were entered.");
                    RehydrateAdjustVm(vm);
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

            var itemIds = _context.Inventories
                .Where(i => ids.Contains(i.InventoryID))
                .Select(i => i.ItemID)
                .Distinct()
                .ToList();

            // Determine costing method of the first item
            var firstItemCostingMethod = _context.Items
                .Where(i => i.ItemID == itemIds.First())
                .Select(i => i.CostingMethod)
                .First();
            // 🔒 FIFO cannot be bulk stock-in (requires per-item unit cost)
            if (firstItemCostingMethod == CostingMethod.FIFO && itemIds.Count > 1)
            {
                TempData["Error"] =
                    "FIFO items must be stocked in one item at a time because each batch requires its own unit cost.";
                return RedirectToAction(nameof(StockIndex));
            }

            // 🔒 Only Fixed / Weighted Average require SAME ITEM
            if ((firstItemCostingMethod == CostingMethod.Fixed ||
                 firstItemCostingMethod == CostingMethod.WeightedAverage)
                && itemIds.Count > 1)
            {
                TempData["Error"] =
                    "Bulk stock-in applies the same quantity, unit, and expiry to all selected rows.\r\nFor Fixed or Weighted Average costing, please stock in one item at a time.";
                return RedirectToAction(nameof(StockIndex));
            }


            var costingMethods =
            (
                from inv in _context.Inventories
                join item in _context.Items on inv.ItemID equals item.ItemID
                where ids.Contains(inv.InventoryID)
                select item.CostingMethod
            ).Distinct().ToList();

            if (costingMethods.Count > 1)
            {
                TempData["Error"] =
                    "Stock-in failed. Only items with the SAME costing method (FIFO, Fixed, or Weighted Average) can be added together. Please stock in separately.";
                return RedirectToAction(nameof(StockIndex));
            }

            var previewItems =
                (from inv in _context.Inventories
                 join item in _context.Items on inv.ItemID equals item.ItemID
                 where ids.Contains(inv.InventoryID)
                 select new AddStockPreviewItem
                 {
                     InventoryID = inv.InventoryID,
                     ItemName = item.ItemName
                 }).ToList();

            var firstInventoryId = ids.First();

            var itemId = _context.Inventories
                .Where(i => i.InventoryID == firstInventoryId)
                .Select(i => i.ItemID)
                .First();

            var units = _context.ItemUnitConversions
                .Where(u => u.ItemID == itemId)
                .OrderByDescending(u => u.IsBaseUnit)
                .ToList();

            bool hasBaseUnit = units.Any(u => u.IsBaseUnit);

            var costingMethod = _context.Items
                .Where(i => i.ItemID == itemId)
                .Select(i => i.CostingMethod)
                .First();

            var suppliers = _context.Suppliers
                .Where(s => s.SupplierStatus == "Active")
                .OrderBy(s => s.SupplierName)
                .ToList();

            var vm = new AddStockBatchViewModel
            {
                InventoryIds = ids,
                PreviewItems = previewItems,
                AvailableUnits = units,
                CostingMethod = costingMethod,
                Suppliers = suppliers,
                HasBaseUnit = hasBaseUnit,

                InputQuantity = null,
                LeadTimeDays = null,
                UnitConversionID = null,
                ExpiryDate = null
            };

            return View(vm);
        }

        // ============================
        // ADD STOCK (POST)
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStockBatch(AddStockBatchViewModel vm)
        {
            vm.PreviewItems = BuildAddStockPreview(vm.InventoryIds);

            var firstInventoryId = vm.InventoryIds.First();

            var itemId = _context.Inventories
                .Where(i => i.InventoryID == firstInventoryId)
                .Select(i => i.ItemID)
                .First();

            // ✅ Rehydrate CostingMethod from DB so it never becomes 0
            vm.CostingMethod = _context.Items
                .Where(i => i.ItemID == itemId)
                .Select(i => i.CostingMethod)
                .First();

            // ✅ Reload units (view uses it)
            vm.AvailableUnits = _context.ItemUnitConversions
                .Where(u => u.ItemID == itemId)
                .OrderByDescending(u => u.IsBaseUnit)
                .ThenBy(u => u.UnitName)
                .ToList();

            vm.HasBaseUnit = vm.AvailableUnits.Any(u => u.IsBaseUnit);

            if ((vm.CostingMethod == CostingMethod.Fixed ||
                 vm.CostingMethod == CostingMethod.WeightedAverage) &&
                !vm.HasBaseUnit)
            {
                ModelState.AddModelError("", "Base unit is missing for this item. Please fix unit setup.");
            }

            vm.Suppliers = _context.Suppliers
                .Where(s => s.SupplierStatus == "Active")
                .OrderBy(s => s.SupplierName)
                .ToList();

            if (vm.SupplierID.HasValue)
            {
                vm.SupplierNameSnapshot = _context.Suppliers
                    .Where(s => s.SupplierID == vm.SupplierID.Value)
                    .Select(s => s.SupplierName)
                    .FirstOrDefault();
            }

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
                    ModelState.AddModelError("", "One or more selected items are inactive or no longer exist.");
                    return View(vm);
                }
            }

            var postCostingMethods =
            (
                from inv in _context.Inventories
                join item in _context.Items on inv.ItemID equals item.ItemID
                where vm.InventoryIds.Contains(inv.InventoryID)
                select item.CostingMethod
            ).Distinct().ToList();

            if (postCostingMethods.Count > 1)
            {
                ModelState.AddModelError("", "Only items with the SAME costing method (FIFO, Fixed, or Weighted Average) can be added together. Please stock in separately.");
                return View(vm);
            }

            if (!ModelState.IsValid)
                return View(vm);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            foreach (var inventoryId in vm.InventoryIds)
            {
                var invData = await
                    (from inv in _context.Inventories
                     join item in _context.Items on inv.ItemID equals item.ItemID
                     where inv.InventoryID == inventoryId
                     select new { inv, item })
                    .FirstAsync();

                if (invData.item.ItemStatus != "Active")
                {
                    ModelState.AddModelError("", $"Item '{invData.item.ItemName}' is inactive and cannot receive stock.");
                    return View(vm);
                }
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var inventoryId in vm.InventoryIds)
                {
                    var invData =
                        await (from inventory in _context.Inventories
                               join itm in _context.Items on inventory.ItemID equals itm.ItemID
                               where inventory.InventoryID == inventoryId
                               select new { Inventory = inventory, Item = itm })
                        .FirstOrDefaultAsync();

                    if (invData == null)
                        continue;

                    var inv = invData.Inventory;
                    var item = invData.Item;

                    if (!vm.InputQuantity.HasValue || vm.InputQuantity.Value <= 0)
                    {
                        ModelState.AddModelError(nameof(vm.InputQuantity), "Quantity is required.");
                        return View(vm);
                    }
                    if (!vm.UnitConversionID.HasValue || vm.UnitConversionID.Value <= 0)
                    {
                        ModelState.AddModelError(nameof(vm.UnitConversionID), "Please select a unit.");
                        return View(vm);
                    }

                    int inQty;
                    try
                    {
                        inQty = ValidateAndConvertToBase(
                            item.ItemID,
                            vm.InputQuantity!.Value,
                            vm.UnitConversionID!.Value,
                            item.CostingMethod
                        );
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", ex.Message);
                        return View(vm);
                    }


                    var batch = new StockBatch
                    {
                        ItemID = item.ItemID,
                        BatchNumber = $"B{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 20),
                        BatchQuantity = inQty,
                        BatchExpiryDate = vm.ExpiryDate!.Value,
                        BatchReceivedDate = DateTime.Now,

                        TransactionUnitCost = item.CostingMethod switch
                        {
                            CostingMethod.Fixed => inv.StandardUnitCost,
                            CostingMethod.WeightedAverage => vm.UnitCost ?? inv.AverageUnitCost,
                            CostingMethod.FIFO => vm.UnitCost!.Value,
                            _ => throw new InvalidOperationException()
                        },

                        SupplierID = vm.SupplierID,
                        SupplierNameSnapshot = vm.SupplierNameSnapshot,
                        LeadTimeDays = vm.LeadTimeDays ?? 0
                    };

                    _context.StockBatches.Add(batch);
                    await _context.SaveChangesAsync();

                    int oldQty = inv.InventoryTotalQuantity;
                    decimal oldAvg = inv.AverageUnitCost;

                    decimal inCost = item.CostingMethod switch
                    {
                        CostingMethod.Fixed => inv.StandardUnitCost,
                        CostingMethod.WeightedAverage => inv.AverageUnitCost,
                        CostingMethod.FIFO => vm.UnitCost ?? throw new InvalidOperationException("Unit cost is required for FIFO."),
                        _ => throw new InvalidOperationException("Unsupported costing method.")
                    };

                    int newQty = oldQty + inQty;

                    if (item.CostingMethod == CostingMethod.WeightedAverage)
                    {
                        if (oldQty == 0)
                        {
                            inv.AverageUnitCost = inCost;
                        }
                        else
                        {
                            decimal oldValue = oldQty * oldAvg;
                            decimal newValue = inQty * inCost;
                            inv.AverageUnitCost = (oldValue + newValue) / (oldQty + inQty);
                        }

                        inv.AverageUnitCost = Math.Round(inv.AverageUnitCost, 4, MidpointRounding.AwayFromZero);
                    }

                    inv.InventoryTotalQuantity = newQty;

                    if (item.CostingMethod == CostingMethod.WeightedAverage)
                    {
                        inv.TotalStockValue = Math.Round(inv.AverageUnitCost * newQty, 2, MidpointRounding.AwayFromZero);
                    }
                    else if (item.CostingMethod == CostingMethod.Fixed)
                    {
                        inv.TotalStockValue = Math.Round(inv.StandardUnitCost * newQty, 2, MidpointRounding.AwayFromZero);
                    }

                    inv.InventoryLastUpdated = DateTime.Now;
                    inv.LastCostUpdated = DateTime.Now;

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

        // ============================
        // MINUS STOCK (GET)
        // ============================
        [HttpGet]
        public IActionResult MinusStockBatchBulk(string inventoryIds)
        {
            if (string.IsNullOrWhiteSpace(inventoryIds))
                return RedirectToAction(nameof(StockIndex));

            var ids = inventoryIds.Split(',').Select(int.Parse).ToList();

            var costingMethods =
            (
                from inv in _context.Inventories
                join item in _context.Items on inv.ItemID equals item.ItemID
                where ids.Contains(inv.InventoryID)
                select item.CostingMethod
            ).Distinct().ToList();

            if (costingMethods.Count > 1)
            {
                TempData["Error"] =
                    "Stock-out failed. Only items with the SAME costing method (FIFO, Fixed, or Weighted Average) can be deducted together.";
                return RedirectToAction(nameof(StockIndex));
            }

            var preview =
                from inv in _context.Inventories
                join item in _context.Items on inv.ItemID equals item.ItemID
                where ids.Contains(inv.InventoryID) && item.ItemStatus == "Active"
                select new BulkMinusPreviewRow
                {
                    InventoryID = inv.InventoryID,
                    ItemName = item.ItemName,
                    AvailableQuantity = inv.InventoryTotalQuantity
                };

            bool hasAnyStock = preview.Any(p => p.AvailableQuantity > 0);


            var firstInventoryId = ids.First();

            var itemId = _context.Inventories
                .Where(i => i.InventoryID == firstInventoryId)
                .Select(i => i.ItemID)
                .First();

            var costingMethod = _context.Items
                .Where(i => i.ItemID == itemId)
                .Select(i => i.CostingMethod)
                .First();

            var units = _context.ItemUnitConversions
                .Where(u => u.ItemID == itemId)
                .OrderByDescending(u => u.IsBaseUnit)
                .ToList();

            var customers = _context.Customers
                .OrderBy(c => c.CustomerName)
                .ToList();

            return View(new BulkMinusStockViewModel
            {
                InventoryIds = ids,
                PreviewItems = preview.ToList(),
                AvailableUnits = units,
                Customers = customers,
                CostingMethod = costingMethod,
                HasAnyStock = hasAnyStock
            });
        }

        // ============================
        // MINUS STOCK (POST)
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MinusStockBatchBulk(BulkMinusStockViewModel vm)
        {
            if (vm.InventoryIds == null || !vm.InventoryIds.Any())
                return RedirectToAction(nameof(StockIndex));

            // ✅ Always rebuild preview for returning View(vm)
            vm.PreviewItems = BuildMinusPreview(vm.InventoryIds);

            // ✅ Ensure lists not null (important)
            vm.FifoConsumptions ??= new List<FifoConsumptionRow>();

            var postCostingMethods =
            (
                from inv in _context.Inventories
                join item in _context.Items on inv.ItemID equals item.ItemID
                where vm.InventoryIds.Contains(inv.InventoryID)
                select item.CostingMethod
            ).Distinct().ToList();

            if (postCostingMethods.Count > 1)
            {
                ModelState.AddModelError("", "Only items with the SAME costing method (FIFO, Fixed, or Weighted Average) can be deducted together.");
                // ✅ rehydrate dropdowns before return view
                RehydrateMinusVm(vm);
                return View(vm);
            }

            var firstInventoryId = vm.InventoryIds.First();
            var itemId = _context.Inventories
                .Where(i => i.InventoryID == firstInventoryId)
                .Select(i => i.ItemID)
                .First();

            // ✅ SOURCE OF TRUTH: always rehydrate costing method so it never becomes 0
            vm.CostingMethod = _context.Items
                .Where(i => i.ItemID == itemId)
                .Select(i => i.CostingMethod)
                .First();

            // ✅ Reload dropdown sources (view uses them)
            vm.AvailableUnits = _context.ItemUnitConversions
                .Where(u => u.ItemID == itemId)
                .OrderByDescending(u => u.IsBaseUnit)
                .ToList();

            vm.Customers = _context.Customers
                .OrderBy(c => c.CustomerName)
                .ToList();

            // 🔒 FORCE BASE UNIT for Fixed/Weighted
            if (vm.CostingMethod == CostingMethod.Fixed || vm.CostingMethod == CostingMethod.WeightedAverage)
            {
                var baseUnit = vm.AvailableUnits.FirstOrDefault(u => u.IsBaseUnit);
                if (baseUnit == null)
                {
                    ModelState.AddModelError("", "Base unit is missing for this item. Please fix unit setup.");
                    return View(vm);
                }
                vm.UnitConversionID = baseUnit.ItemUnitConversionID;
            }

            if (!ModelState.IsValid)
                return View(vm);

            if (vm.UnitConversionID <= 0)
            {
                ModelState.AddModelError(nameof(vm.UnitConversionID), "Please select a unit. Quantity will be converted to base units.");
                return View(vm);
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            string? customerSnapshot = null;
            if (vm.CustomerID.HasValue)
            {
                customerSnapshot = _context.Customers
                    .Where(c => c.CustomerID == vm.CustomerID.Value)
                    .Select(c => c.CustomerName)
                    .FirstOrDefault();
            }
            else if (!string.IsNullOrWhiteSpace(vm.CustomerNameSnapshot))
            {
                customerSnapshot = vm.CustomerNameSnapshot.Trim();
            }

            using var tx = _context.Database.BeginTransaction();

            try
            {
                foreach (var inventoryId in vm.InventoryIds)
                {
                    var inv = _context.Inventories.First(i => i.InventoryID == inventoryId);

                    if (IsItemInactive(inv.ItemID))
                        continue;

                    var item = _context.Items.First(i => i.ItemID == inv.ItemID);

                    int originalDeductQty;
                    try
                    {
                        originalDeductQty = ValidateAndConvertToBase(
                            inv.ItemID,
                            vm.InputQuantity,
                            vm.UnitConversionID,
                            item.CostingMethod
                        );
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", ex.Message);
                        return View(vm);
                    }

                    vm.BaseQuantity = originalDeductQty;

                    int remaining = originalDeductQty;

                    if (remaining > inv.InventoryTotalQuantity)
                    {
                        ModelState.AddModelError("", $"Insufficient stock for item ID {inv.ItemID}.");
                        return View(vm);
                    }

                    

                    var batchesQuery = _context.StockBatches
                        .Where(b => b.ItemID == inv.ItemID && b.BatchQuantity > 0);

                    if (item.CostingMethod == CostingMethod.FIFO)
                    {
                        batchesQuery = batchesQuery
                            .OrderBy(b => b.BatchReceivedDate)
                            .ThenBy(b => b.BatchID);
                    }

                    var batches = batchesQuery.ToList();

                    decimal outUnitCost = item.CostingMethod switch
                    {
                        CostingMethod.WeightedAverage => Math.Round(inv.AverageUnitCost, 4),
                        CostingMethod.Fixed => Math.Round(inv.StandardUnitCost, 4),
                        _ => 0m
                    };

                    foreach (var batch in batches)
                    {
                        if (remaining <= 0)
                            break;

                        int deductQty = Math.Min(batch.BatchQuantity, remaining);

                        batch.BatchQuantity -= deductQty;
                        remaining -= deductQty;

                        vm.FifoConsumptions.Add(new FifoConsumptionRow
                        {
                            BatchNumber = batch.BatchNumber,
                            ExpiryDate = batch.BatchExpiryDate,
                            QuantityConsumed = deductQty
                        });

                        _context.StockTransactions.Add(new StockTransaction
                        {
                            UserID = userId,
                            ItemID = inv.ItemID,
                            BatchID = batch.BatchID,
                            TransactionType = "OUT",
                            TransactionQuantity = deductQty,
                            UnitCost = item.CostingMethod == CostingMethod.FIFO ? batch.TransactionUnitCost : outUnitCost,
                            CostingMethodUsed = item.CostingMethod,
                            CustomerID = vm.CustomerID,
                            CustomerNameSnapshot = customerSnapshot,
                            TransactionRemark = vm.StockOutRemark
                        });
                    }

                    inv.InventoryTotalQuantity -= originalDeductQty;

                    if (item.CostingMethod == CostingMethod.WeightedAverage)
                    {
                        if (inv.InventoryTotalQuantity == 0)
                        {
                            inv.TotalStockValue = 0;
                        }
                        else
                        {
                            inv.TotalStockValue = Math.Round(inv.InventoryTotalQuantity * inv.AverageUnitCost, 2, MidpointRounding.AwayFromZero);
                        }
                    }
                    else if (item.CostingMethod == CostingMethod.Fixed)
                    {
                        if (inv.InventoryTotalQuantity == 0)
                            inv.TotalStockValue = 0;
                        else
                            inv.TotalStockValue = Math.Round(inv.InventoryTotalQuantity * inv.StandardUnitCost, 2, MidpointRounding.AwayFromZero);
                    }

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

        // ✅ helper: ensure dropdown data exists before returning View(vm)
        private void RehydrateMinusVm(BulkMinusStockViewModel vm)
        {
            if (vm.InventoryIds == null || !vm.InventoryIds.Any()) return;

            var firstInventoryId = vm.InventoryIds.First();
            var itemId = _context.Inventories
                .Where(i => i.InventoryID == firstInventoryId)
                .Select(i => i.ItemID)
                .First();

            vm.CostingMethod = _context.Items
                .Where(i => i.ItemID == itemId)
                .Select(i => i.CostingMethod)
                .First();

            vm.AvailableUnits = _context.ItemUnitConversions
                .Where(u => u.ItemID == itemId)
                .OrderByDescending(u => u.IsBaseUnit)
                .ToList();

            vm.Customers = _context.Customers
                .OrderBy(c => c.CustomerName)
                .ToList();

            vm.FifoConsumptions ??= new List<FifoConsumptionRow>();
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
                join b in _context.StockBatches on d.BatchID equals b.BatchID
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

                    SupplierName = t.TransactionType == "IN" ? batch.SupplierNameSnapshot : null,
                    LeadTimeDays = t.TransactionType == "IN" ? batch.LeadTimeDays : null,
                    ExpectedArrivalDate = t.TransactionType == "IN"
                        ? batch.BatchReceivedDate.AddDays(batch.LeadTimeDays)
                        : null,

                    CustomerName = t.TransactionType == "OUT" ? t.CustomerNameSnapshot : null,
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

        // =====================================================
        // MULTI-UOM – CONVERT INPUT UNIT → BASE UNIT
        // =====================================================
        private int ConvertToBaseUnit(int itemId, int inputQty, int unitConversionId)
        {
            if (inputQty == 0)
                throw new InvalidOperationException("Quantity cannot be zero.");

            var conversion = _context.ItemUnitConversions
                .FirstOrDefault(u =>
                    u.ItemUnitConversionID == unitConversionId &&
                    u.ItemID == itemId);

            if (conversion == null)
                throw new InvalidOperationException("Invalid unit conversion selected.");

            checked
            {
                return inputQty * conversion.BaseUnitMultiplier;
            }
        }

        // =====================================================
        // 🔒 VALIDATE UNIT + CONVERT TO BASE (AUTHORITATIVE)
        // =====================================================
        private int ValidateAndConvertToBase(
            int itemId,
            int inputQty,
            int unitConversionId,
            CostingMethod costingMethod)
        {
            if (inputQty <= 0)
                throw new InvalidOperationException("Quantity must be greater than zero.");

            var unit = _context.ItemUnitConversions
                .FirstOrDefault(u =>
                    u.ItemUnitConversionID == unitConversionId &&
                    u.ItemID == itemId);

            if (unit == null)
                throw new InvalidOperationException("Invalid unit selected.");

            // 🔒 Fixed + Weighted → base unit only
            if ((costingMethod == CostingMethod.Fixed ||
                 costingMethod == CostingMethod.WeightedAverage) &&
                !unit.IsBaseUnit)
            {
                throw new InvalidOperationException(
                    "This costing method only allows base unit."
                );
            }

            checked
            {
                return inputQty * unit.BaseUnitMultiplier;
            }
        }

        // =====================================================
        // 🔓 VALIDATE UNIT + CONVERT TO BASE (ALLOW NEGATIVE)
        // 👉 USED ONLY FOR ADJUST STOCK
        // =====================================================
        private int ValidateAndConvertToBaseAllowNegative(
            int itemId,
            int inputQty,
            int unitConversionId,
            CostingMethod costingMethod)
        {
            if (inputQty == 0)
                throw new InvalidOperationException("Quantity cannot be zero.");

            var unit = _context.ItemUnitConversions
                .FirstOrDefault(u =>
                    u.ItemUnitConversionID == unitConversionId &&
                    u.ItemID == itemId);

            if (unit == null)
                throw new InvalidOperationException("Invalid unit selected.");

            // 🔒 Fixed + Weighted → base unit only
            if ((costingMethod == CostingMethod.Fixed ||
                 costingMethod == CostingMethod.WeightedAverage) &&
                !unit.IsBaseUnit)
            {
                throw new InvalidOperationException(
                    "This costing method only allows base unit."
                );
            }

            checked
            {
                return inputQty * unit.BaseUnitMultiplier;
            }
        }

        private void RehydrateAdjustVm(AdjustStockByBatchViewModel vm)
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

            vm.Customers = _context.Customers
                .OrderBy(c => c.CustomerName)
                .ToList();
        }


        public IActionResult SupplierStockInSummary()
        {
            var summary =
                from b in _context.StockBatches
                where b.SupplierID != null
                group b by new { b.SupplierID, b.SupplierNameSnapshot } into g
                select new SupplierStockInSummaryViewModel
                {
                    SupplierID = g.Key.SupplierID!.Value,
                    SupplierName = g.Key.SupplierNameSnapshot ?? "(Unknown)",
                    TotalQuantityReceived = g.Sum(x => x.BatchQuantity),
                    TotalPayableValue = g.Sum(x => x.BatchQuantity * x.TransactionUnitCost),
                    AverageLeadTimeDays = (int)Math.Round(g.Average(x => (double)x.LeadTimeDays)),
                    LastDeliveryDate = g.Max(x => x.BatchReceivedDate)
                };

            return View(summary
                .OrderByDescending(s => s.TotalPayableValue)
                .ToList());
        }
    }
}

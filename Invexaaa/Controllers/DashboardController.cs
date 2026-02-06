using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Invexaaa.Data;
using Invexaaa.Models.ViewModels;

namespace Invexaaa.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly InvexaDbContext _context;

        public DashboardController(InvexaDbContext context)
        {
            _context = context;
        }
        
        public IActionResult Index()
        {
            var model = new DashboardViewModel();

            // TOTAL ITEMS (WITH BREAKDOWN)
            model.TotalItems = _context.Items.Count();
            model.ActiveItemCount = _context.Items.Count(i => i.ItemStatus == "Active");
            model.InactiveItemCount = _context.Items.Count(i => i.ItemStatus == "Inactive");

            // JOIN Items + Inventory
            var inventoryData =
                from i in _context.Items
                join inv in _context.Inventories
                    on i.ItemID equals inv.ItemID
                select new
                {
                    i.ItemName,
                    i.ItemStatus,
                    inv.InventoryTotalQuantity,
                    i.ItemReorderLevel,
                    i.ReorderPoint,
                    inv.InventoryLastUpdated
                };

            // STOCK COUNTS (ACTIVE ONLY)
            model.OutOfStockCount =
                inventoryData.Count(x =>
                    x.ItemStatus == "Active" &&
                    x.InventoryTotalQuantity == 0);

            model.ReorderAlertCount =
                inventoryData.Count(x =>
                    x.ItemStatus == "Active" &&
                    x.InventoryTotalQuantity <= x.ReorderPoint);

            model.LowStockCount =
                inventoryData.Count(x =>
                    x.ItemStatus == "Active" &&
                    x.InventoryTotalQuantity > x.ReorderPoint &&
                    x.InventoryTotalQuantity <= x.ItemReorderLevel);

            model.OkStockCount =
                inventoryData.Count(x =>
                    x.ItemStatus == "Active" &&
                    x.InventoryTotalQuantity > x.ItemReorderLevel);

            // RECENT INVENTORY (SHOW INACTIVE CLEARLY)
            model.RecentInventories =
                inventoryData
                .OrderByDescending(x => x.InventoryLastUpdated)
                .Take(5)
                .Select(x => new InventoryRow
                {
                    ItemName = x.ItemName,
                    Quantity = x.InventoryTotalQuantity,
                    ItemStatus = x.ItemStatus,
                    Status =
                        x.ItemStatus == "Inactive" ? "Locked" :
                        x.InventoryTotalQuantity <= x.ReorderPoint ? "Reorder" :
                        x.InventoryTotalQuantity <= x.ItemReorderLevel ? "Low" :
                        "OK"
                })
                .ToList();

            // ===============================
            // REORDER PLANNER (DEMAND-BASED)
            // ===============================
            const int PLANNING_DAYS = 14;

            model.ReorderPlanner =
            (
                from i in _context.Items
                join inv in _context.Inventories
                    on i.ItemID equals inv.ItemID
                where i.ItemStatus == "Active"
                      && i.AverageDailyDemand > 0
                let targetStock =
                    (int)Math.Ceiling(
                        (i.AverageDailyDemand * PLANNING_DAYS) + i.SafetyStock
                    )
                let suggestedQty =
                    targetStock - inv.InventoryTotalQuantity
                select new ReorderPlannerItemVm
                {
                    ItemName = i.ItemName,

                    CurrentQty = inv.InventoryTotalQuantity,

                    ReorderPoint = i.ReorderPoint,
                    SafetyStock = i.SafetyStock,
                    AverageDailyDemand = i.AverageDailyDemand,

                    TargetStock = targetStock,
                    SuggestedOrderQty = suggestedQty > 0 ? suggestedQty : 0,

                    RunoutDays =
                        i.AverageDailyDemand > 0
                            ? Math.Round(
                                inv.InventoryTotalQuantity / i.AverageDailyDemand, 1)
                            : null
                }
            )
            .Where(x => x.SuggestedOrderQty > 0)
            .OrderBy(x => x.RunoutDays)
            .Take(10)
            .ToList();

            return View(model);


        }
    }
}

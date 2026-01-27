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


            return View(model);
        }
    }
}

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

            // TOTAL ITEMS
            model.TotalItems = _context.Items.Count();

            // JOIN Items + Inventory
            var inventoryData =
                from i in _context.Items
                join inv in _context.Inventories
                    on i.ItemID equals inv.ItemID
                select new
                {
                    i.ItemName,
                    inv.InventoryTotalQuantity,
                    i.ItemReorderLevel,
                    i.ReorderPoint,
                    inv.InventoryLastUpdated
                };

            // OUT OF STOCK
            model.OutOfStockCount =
                inventoryData.Count(x => x.InventoryTotalQuantity == 0);

            // LOW STOCK
            model.LowStockCount =
                inventoryData.Count(x =>
                    x.InventoryTotalQuantity > 0 &&
                    x.InventoryTotalQuantity <= x.ItemReorderLevel);

            // REORDER ALERT
            model.ReorderAlertCount =
                inventoryData.Count(x =>
                    x.InventoryTotalQuantity <= x.ReorderPoint);

            // RECENT INVENTORY ACTIVITY (TOP 5)
            model.RecentInventories =
                inventoryData
                .OrderByDescending(x => x.InventoryLastUpdated)
                .Take(5)
                .Select(x => new InventoryRow
                {
                    ItemName = x.ItemName,
                    Quantity = x.InventoryTotalQuantity,
                    Status =
                        x.InventoryTotalQuantity == 0 ? "Out" :
                        x.InventoryTotalQuantity <= x.ItemReorderLevel ? "Low" :
                        "OK"
                })
                .ToList();

            return View(model);
        }
    }
}

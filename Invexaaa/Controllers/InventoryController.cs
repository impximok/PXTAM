using Invexaaa.Data;
using Invexaaa.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Invexaaa.Controllers
{
    public class InventoryController : Controller
    {
        private readonly InvexaDbContext _context;

        public InventoryController(InvexaDbContext context)
        {
            _context = context;
        }

        // MANAGEMENT VIEW (NOT STOCK OPS)
        public IActionResult InventoryIndex()
        {
            var list =
                from inv in _context.Inventories
                join item in _context.Items on inv.ItemID equals item.ItemID
                select new InventoryOverviewViewModel
                {
                    InventoryID = inv.InventoryID,
                    ItemID = inv.ItemID,
                    ItemName = item.ItemName,
                    TotalQuantity = inv.InventoryTotalQuantity,
                    HealthStatus =
    inv.InventoryTotalQuantity == 0 ? "Critical" :
    inv.InventoryTotalQuantity <= item.ItemReorderLevel ? "Low" :
    "Healthy",

                    LastUpdated = inv.InventoryLastUpdated,
                    ItemStatus = item.ItemStatus
                };

            return View("InventoryIndex", list.ToList());
        }
    }
}

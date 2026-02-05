using Invexaaa.Data;
using Invexaaa.Models.Invexa.Enums;
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
                    CostingMethod = item.CostingMethod,
                    StandardUnitCost = inv.StandardUnitCost,

                    LastUpdated = inv.InventoryLastUpdated,
                    ItemStatus = item.ItemStatus
                };

            return View("InventoryIndex", list.ToList());
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var data =
                (from inv in _context.Inventories
                 join item in _context.Items on inv.ItemID equals item.ItemID
                 where inv.InventoryID == id
                 select new InventoryEditViewModel
                 {
                     InventoryID = inv.InventoryID,
                     ItemID = item.ItemID,
                     ItemName = item.ItemName,
                     CurrentQuantity = inv.InventoryTotalQuantity,
                     CostingMethod = item.CostingMethod,
                     StandardUnitCost = inv.StandardUnitCost,
                     TotalStockValue = inv.TotalStockValue,
                     LastCostUpdated = inv.LastCostUpdated
                 }).FirstOrDefault();

            if (data == null)
                return NotFound();

            // 🔒 HARD BLOCK — only Fixed costing allowed
            if (data.CostingMethod != CostingMethod.Fixed)
                return BadRequest("Only Fixed costing items can edit standard cost.");

            return View("EditInventory", data);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(InventoryEditViewModel vm)
        {
            if (!ModelState.IsValid)
                return View("EditInventory", vm);


            var inv = _context.Inventories.FirstOrDefault(i => i.InventoryID == vm.InventoryID);
            if (inv == null)
                return NotFound();

            var item = _context.Items.First(i => i.ItemID == inv.ItemID);

            // 🔒 Double guard
            if (item.CostingMethod != CostingMethod.Fixed)
                return BadRequest("Invalid costing method.");

            inv.StandardUnitCost = vm.StandardUnitCost;

            inv.TotalStockValue = Math.Round(
                inv.StandardUnitCost * inv.InventoryTotalQuantity,
                2,
                MidpointRounding.AwayFromZero
            );

            inv.LastCostUpdated = DateTime.Now;
            inv.InventoryLastUpdated = DateTime.Now;

            _context.SaveChanges();

            TempData["Success"] = "Standard cost updated successfully.";
            return RedirectToAction(nameof(InventoryIndex));
        }


    }
}

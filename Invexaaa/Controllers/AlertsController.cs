using Microsoft.AspNetCore.Mvc;
using Invexaaa.Data;
using Invexaaa.Models.ViewModels;

namespace Invexaaa.Controllers
{
    public class AlertsController : Controller
    {
        private readonly InvexaDbContext _context;

        public AlertsController(InvexaDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var today = DateTime.Today;
            var nearExpiryThreshold = today.AddDays(7);

            var viewModel = new AlertsDashboardViewModel
            {
                // 🔴 EXPIRED BATCHES (ACTIVE + INACTIVE)
                ExpiredItems =
                    (from b in _context.StockBatches
                     join i in _context.Items on b.ItemID equals i.ItemID
                     where b.BatchQuantity > 0 &&
                           b.BatchExpiryDate < today
                     select new ExpiryTrackingViewModel
                     {
                         BatchID = b.BatchID,
                         BatchNumber = b.BatchNumber,
                         Quantity = b.BatchQuantity,
                         ExpiryDate = b.BatchExpiryDate,
                         ExpiryStatus = "Expired",
                         ItemStatus = i.ItemStatus   // ✅ PASS STATUS
                     }).ToList(),

                // 🟠 NEAR EXPIRY BATCHES (ACTIVE + INACTIVE)
                NearExpiryItems =
                    (from b in _context.StockBatches
                     join i in _context.Items on b.ItemID equals i.ItemID
                     where b.BatchQuantity > 0 &&
                           b.BatchExpiryDate >= today &&
                           b.BatchExpiryDate <= nearExpiryThreshold
                     select new ExpiryTrackingViewModel
                     {
                         BatchID = b.BatchID,
                         BatchNumber = b.BatchNumber,
                         Quantity = b.BatchQuantity,
                         ExpiryDate = b.BatchExpiryDate,
                         ExpiryStatus = "Near Expiry",
                         ItemStatus = i.ItemStatus   // ✅ PASS STATUS
                     }).ToList(),

                // 🟡 LOW STOCK (ACTIVE + INACTIVE)
                LowStockItems =
                    (from inv in _context.Inventories
                     join item in _context.Items on inv.ItemID equals item.ItemID
                     where inv.InventoryTotalQuantity > item.ReorderPoint &&
                           inv.InventoryTotalQuantity <= item.ItemReorderLevel
                     select new InventoryOverviewViewModel
                     {
                         InventoryID = inv.InventoryID,
                         ItemID = inv.ItemID,
                         ItemName = item.ItemName,
                         TotalQuantity = inv.InventoryTotalQuantity,
                         HealthStatus = "Low",
                         ItemStatus = item.ItemStatus,   // ✅ REQUIRED
                         LastUpdated = inv.InventoryLastUpdated
                     }).ToList(),

                // 🔵 REORDER REQUIRED (ACTIVE + INACTIVE)
                ReorderItems =
                    (from inv in _context.Inventories
                     join item in _context.Items on inv.ItemID equals item.ItemID
                     where inv.InventoryTotalQuantity <= item.ReorderPoint
                     select new ItemCardViewModel
                     {
                         InventoryID = inv.InventoryID,
                         ItemID = item.ItemID,
                         ItemName = item.ItemName,
                         ItemSellPrice = item.ItemSellPrice,
                         ItemStatus = item.ItemStatus,   // ✅ DO NOT OVERRIDE
                         ItemImageUrl = item.ItemImageUrl,
                         ItemBarcode = item.ItemBarcode,
                         ReorderLevel = item.ItemReorderLevel,
                         SafetyStock = item.SafetyStock,
                         CurrentBalance = inv.InventoryTotalQuantity
                     }).ToList()
            };

            return View("AlertsIndex", viewModel);
        }


    }
}

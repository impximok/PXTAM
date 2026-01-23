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
                // 🔴 Expired Batches
                ExpiredItems = _context.StockBatches
                    .Where(b => b.BatchExpiryDate < today)
                    .Select(b => new ExpiryTrackingViewModel
                    {
                        BatchID = b.BatchID,
                        BatchNumber = b.BatchNumber,
                        Quantity = b.BatchQuantity,
                        ExpiryDate = b.BatchExpiryDate,
                        ExpiryStatus = "Expired"
                    })
                    .ToList(),

                // 🟠 Near Expiry Batches
                NearExpiryItems = _context.StockBatches
                    .Where(b => b.BatchExpiryDate >= today &&
                                b.BatchExpiryDate <= nearExpiryThreshold)
                    .Select(b => new ExpiryTrackingViewModel
                    {
                        BatchID = b.BatchID,
                        BatchNumber = b.BatchNumber,
                        Quantity = b.BatchQuantity,
                        ExpiryDate = b.BatchExpiryDate,
                        ExpiryStatus = "Near Expiry"
                    })
                    .ToList(),

                // 🟡 Low Stock Items
                LowStockItems = _context.Inventories
                    .Where(i => i.InventoryTotalQuantity <= 10)
                    .Select(i => new InventoryOverviewViewModel
                    {
                        InventoryID = i.InventoryID,
                        ItemID = i.ItemID,
                        TotalQuantity = i.InventoryTotalQuantity,
                        HealthStatus = "Low",
                        LastUpdated = i.InventoryLastUpdated
                    })
                    .ToList(),

                // 🔵 Reorder Required Items
                ReorderItems = _context.Items
                    .Where(i => i.ReorderPoint <= i.ItemReorderLevel)
                    .Select(i => new ItemCardViewModel
                    {
                        ItemID = i.ItemID,
                        ItemName = i.ItemName,

                        ItemSellPrice = i.ItemSellPrice,
                        ItemStatus = "Reorder Needed",
                        ItemImageUrl = i.ItemImageUrl,
                        ItemBarcode = i.ItemBarcode,

                        ReorderLevel = i.ItemReorderLevel,
                        SafetyStock = i.SafetyStock,
                        CurrentBalance = i.ReorderPoint
                    })
                    .ToList()
            };

            return View("AlertsIndex", viewModel);
        }
    }
}

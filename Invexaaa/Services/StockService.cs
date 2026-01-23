using Invexaaa.Data;

namespace Invexaaa.Services
{
    public class StockService
    {
        private readonly InvexaDbContext _context;

        public StockService(InvexaDbContext context)
        {
            _context = context;
        }

        public void AdjustStock(int inventoryId, int newQuantity)
        {
            var inv = _context.Inventories.Find(inventoryId);
            if (inv == null) return;

            inv.InventoryTotalQuantity = newQuantity;
            inv.InventoryLastUpdated = DateTime.Now;
            _context.SaveChanges();
        }
    }
}

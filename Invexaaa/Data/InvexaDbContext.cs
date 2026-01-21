using Microsoft.EntityFrameworkCore;
using Invexaaa.Models.Invexa;

namespace Invexaaa.Data
{
    public class InvexaDbContext : DbContext
    {
        public InvexaDbContext(DbContextOptions<InvexaDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<StockBatch> StockBatches { get; set; }
        public DbSet<StockTransaction> StockTransactions { get; set; }
        public DbSet<StockAdjustment> StockAdjustments { get; set; }
        public DbSet<StockAdjustmentDetail> StockAdjustmentDetails { get; set; }
        public DbSet<DemandForecast> DemandForecasts { get; set; }
        public DbSet<AlertType> AlertTypes { get; set; }
        public DbSet<Alert> Alerts { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<SalesHeader> SalesHeaders { get; set; }
        public DbSet<SalesDetail> SalesDetails { get; set; }
    }

}


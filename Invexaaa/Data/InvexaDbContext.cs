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
        public DbSet<SalesHeader> SalesHeaders { get; set; }
        public DbSet<SalesDetail> SalesDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Item>(entity =>
            {
                entity.Property(e => e.ItemBuyPrice).HasPrecision(18, 2);
                entity.Property(e => e.ItemSellPrice).HasPrecision(18, 2);
                entity.Property(e => e.AverageDailyDemand).HasPrecision(18, 4);
            });

            modelBuilder.Entity<SalesHeader>(entity =>
            {
                entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            });

            modelBuilder.Entity<SalesDetail>(entity =>
            {
                entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
                entity.Property(e => e.Subtotal).HasPrecision(18, 2);
            });
        }
    }
}

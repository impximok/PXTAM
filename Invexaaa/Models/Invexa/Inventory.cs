using System;
using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.Invexa
{
    public class Inventory
    {
        [Key]
        public int InventoryID { get; set; }

        [Required(ErrorMessage = "Item is required.")]
        public int ItemID { get; set; }

        [Required(ErrorMessage = "Total quantity is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Total quantity cannot be negative.")]
        public int InventoryTotalQuantity { get; set; }

        [Required(ErrorMessage = "Last updated date is required.")]
        public DateTime InventoryLastUpdated { get; set; } = DateTime.Now;

        // ✅ OPTIMISTIC CONCURRENCY TOKEN
        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}

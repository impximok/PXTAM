using System;
using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.Invexa
{
    public class StockBatch
    {
        [Key]
        public int BatchID { get; set; }

        [Required(ErrorMessage = "Item is required.")]
        public int ItemID { get; set; }

        [Required(ErrorMessage = "Batch number is required.")]
        [MaxLength(50, ErrorMessage = "Batch number must not exceed 50 characters.")]
        public string BatchNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Batch quantity is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Batch quantity cannot be negative.")]
        public int BatchQuantity { get; set; }

        [Required(ErrorMessage = "Expiry date is required.")]
        public DateTime BatchExpiryDate { get; set; }

        [Required(ErrorMessage = "Received date is required.")]
        public DateTime BatchReceivedDate { get; set; } = DateTime.Now;

        // ✅ CONCURRENCY TOKEN (FIFO deductions)
        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    }
}

using System;
using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.Invexa
{
    public class StockTransaction
    {
        [Key]
        public int TransactionID { get; set; }

        [Required(ErrorMessage = "User is required for the transaction.")]
        public int UserID { get; set; }

        [Required(ErrorMessage = "Item is required for the transaction.")]
        public int ItemID { get; set; }

        // Optional: only for batch-tracked items
        public int? BatchID { get; set; }

        [Required(ErrorMessage = "Transaction type is required.")]
        [MaxLength(10, ErrorMessage = "Transaction type must not exceed 10 characters.")]
        public string TransactionType { get; set; } = "IN"; // IN / OUT

        [Required(ErrorMessage = "Transaction quantity is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Transaction quantity must be at least 1.")]
        public int TransactionQuantity { get; set; }

        [Required(ErrorMessage = "Transaction date is required.")]
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [MaxLength(255, ErrorMessage = "Transaction remark must not exceed 255 characters.")]
        public string? TransactionRemark { get; set; }
    }
}

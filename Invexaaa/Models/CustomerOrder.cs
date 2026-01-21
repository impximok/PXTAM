using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnomiAssignmentReal.Models;

public class CustomerOrder
{
    [Key]
    [MaxLength(10)]
    public string CustomerOrderId { get; set; } // e.g., "O123"

    [Required]
    public string CustomerId { get; set; }
    public Customer Customer { get; set; }

    [Range(0, 100, ErrorMessage = "Table number must be between 1 and 100.")]
    public int TableNumber { get; set; }

    public DateTime OrderCreatedAt { get; set; } = DateTime.Now;

    // TIP: If you support QR "AwaitingPayment", consider defaulting this to null
    public DateTime? PaymentCompletedAt { get; set; } 

    [MaxLength(50)]
    public string OrderStatus { get; set; } = "Ordered";

    [MaxLength(50)]
    public string? PaymentMethodName { get; set; }

    [Range(0.0, 10000.0, ErrorMessage = "OrderTotalAmount must be a positive number.")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal OrderTotalAmount { get; set; }

    public int RewardPointsRedeemed { get; set; } = 0;
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalDiscountAmount { get; set; } = 0m;
    public int RewardPointsEarned { get; set; } = 0;
    [Column(TypeName = "decimal(18,2)")]
    public decimal NetPayableAmount { get; set; } = 0m;
    public DateTime? RewardPointsAwardedAt { get; set; }

    // ✅ NEW: mark when we’ve emailed the receipt (prevents duplicates)
    public DateTime? EmailReceiptSentAt { get; set; }

    // (Optional) If you want receipts for guests, capture their email at checkout:
    // [EmailAddress, MaxLength(255)]
    // public string? ReceiptEmail { get; set; }

    public ICollection<OrderDetail> CustomerOrderDetails { get; set; }
}

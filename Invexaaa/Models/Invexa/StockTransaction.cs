using Invexaaa.Models.Invexa.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.Invexa
{
    public class StockTransaction : IValidatableObject
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
        [RegularExpression("^(IN|OUT)$", ErrorMessage = "Transaction type must be IN or OUT.")]
        public string TransactionType { get; set; } = "IN";

        [Required(ErrorMessage = "Transaction quantity is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Transaction quantity must be at least 1.")]
        public int TransactionQuantity { get; set; }

        // Required for IN, optional (but allowed) for OUT
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit cost must be greater than 0.")]
        public decimal UnitCost { get; set; }
        // ======================
        // COSTING AUDIT
        // ======================
        [Required]
        public CostingMethod CostingMethodUsed { get; set; }


        [Required(ErrorMessage = "Transaction date is required.")]
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [MaxLength(255, ErrorMessage = "Transaction remark must not exceed 255 characters.")]
        public string? TransactionRemark { get; set; }

        // ======================
        // CUSTOMER (OUT ONLY)
        // ======================
        public int? CustomerID { get; set; }

        [MaxLength(100)]
        public string? CustomerNameSnapshot { get; set; }

        // ======================
        // CONDITIONAL VALIDATION
        // ======================
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // OUT → Customer required
            if (TransactionType == "OUT")
            {
                if (!CustomerID.HasValue)
                {
                    yield return new ValidationResult(
                        "Customer is required for stock OUT transactions.",
                        new[] { nameof(CustomerID) }
                    );
                }

                if (string.IsNullOrWhiteSpace(CustomerNameSnapshot))
                {
                    yield return new ValidationResult(
                        "Customer name snapshot is required for stock OUT transactions.",
                        new[] { nameof(CustomerNameSnapshot) }
                    );
                }
            }

            // IN → Customer must NOT be set
            if (TransactionType == "IN" && CustomerID.HasValue)
            {
                yield return new ValidationResult(
                    "Customer cannot be set for stock IN transactions.",
                    new[] { nameof(CustomerID) }
                );
            }

            // IN → Unit cost must be > 0
            if (TransactionType == "IN" && UnitCost <= 0)
            {
                yield return new ValidationResult(
                    "Unit cost is required for stock IN transactions.",
                    new[] { nameof(UnitCost) }
                );
            }
        }
    }
}

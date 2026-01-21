using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.Invexa
{
    public class StockAdjustmentDetail
    {
        [Key]
        public int AdjustmentDetailID { get; set; }

        [Required(ErrorMessage = "Stock adjustment is required.")]
        public int AdjustmentID { get; set; }

        [Required(ErrorMessage = "Item is required.")]
        public int ItemID { get; set; }

        // Optional: only when batch tracking is enabled
        public int? BatchID { get; set; }

        [Required(ErrorMessage = "Quantity before adjustment is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity before cannot be negative.")]
        public int QuantityBefore { get; set; }

        [Required(ErrorMessage = "Quantity after adjustment is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity after cannot be negative.")]
        public int QuantityAfter { get; set; }

        [Required(ErrorMessage = "Quantity difference is required.")]
        public int QuantityDifference { get; set; }
    }
}

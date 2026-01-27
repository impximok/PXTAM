using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.ViewModels
{
    public class AdjustStockByBatchViewModel
    {
        public int InventoryID { get; set; }
        public int ItemID { get; set; }

        public string ItemName { get; set; } = "";
        public string ItemUnitOfMeasure { get; set; } = "";

        public int CurrentInventoryQuantity { get; set; }

        [Required(ErrorMessage = "Adjustment reason is required.")]
        [StringLength(255, ErrorMessage = "Reason cannot exceed 255 characters.")]
        public string AdjustmentReason { get; set; } = "";

        public List<AdjustStockBatchRowViewModel> Batches { get; set; }
            = new();
    }
}

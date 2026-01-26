using System.Collections.Generic;

namespace Invexaaa.Models.ViewModels
{
    public class AdjustStockByBatchViewModel
    {
        public int InventoryID { get; set; }
        public int ItemID { get; set; }

        public string ItemName { get; set; } = "";
        public string ItemUnitOfMeasure { get; set; } = "";

        public int CurrentInventoryQuantity { get; set; }

        public string AdjustmentReason { get; set; } = "";

        public List<AdjustStockBatchRowViewModel> Batches { get; set; }
            = new();
    }
}

using System;

namespace Invexaaa.Models.ViewModels
{
    public class AdjustStockBatchRowViewModel
    {
        public int BatchID { get; set; }
        public string BatchNumber { get; set; } = "";
        public DateTime BatchExpiryDate { get; set; }
        public int AvailableQuantity { get; set; }

        // Can be + or -
        public int AdjustQuantity { get; set; }
    }
}

namespace Invexaaa.Models.ViewModels
{
    public class StockAdjustmentHistoryViewModel
    {
        public int AdjustmentID { get; set; }
        public DateTime AdjustmentDate { get; set; }
        public string AdjustmentReason { get; set; }
        public string AdjustmentStatus { get; set; }

        public string ItemName { get; set; }
        public int QuantityBefore { get; set; }
        public int QuantityAfter { get; set; }
        public int QuantityDifference { get; set; }
    }
}

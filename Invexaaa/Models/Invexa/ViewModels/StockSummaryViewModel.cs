namespace Invexaaa.Models.Invexa.ViewModels
{
    public class StockSummaryViewModel
    {
        public int ItemID { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string ItemStatus { get; set; } = "Active";
        public int Quantity { get; set; }
        public decimal BuyPrice { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}

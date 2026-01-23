namespace Invexaaa.Models.ViewModels
{
    public class StockBatchListViewModel
    {
        public string ItemName { get; set; }
        public string BatchNumber { get; set; }
        public int BatchQuantity { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string BatchStatus { get; set; }
    }
}

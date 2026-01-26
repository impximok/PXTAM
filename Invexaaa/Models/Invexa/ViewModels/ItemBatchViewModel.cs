namespace Invexaaa.Models.ViewModels
{
    public class ItemBatchViewModel
    {
        public string BatchNo { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int Quantity { get; set; }
        // Display-only, computed in controller
        public string ExpiryStatus { get; set; } = "Safe";
    }
}

namespace Invexaaa.Models.ViewModels
{
    public class ItemBatchViewModel
    {
        public string BatchNo { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int Quantity { get; set; }
        public string BatchStatus { get; set; } // Normal / Near Expiry / Reorder
    }
}

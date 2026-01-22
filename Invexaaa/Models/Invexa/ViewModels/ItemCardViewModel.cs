namespace Invexaaa.Models.ViewModels
{
    public class ItemCardViewModel
    {
        public int ItemID { get; set; }
        public string ItemName { get; set; }
        public string CategoryName { get; set; }
        public string SupplierName { get; set; }

        public decimal ItemSellPrice { get; set; }
        public string ItemStatus { get; set; }
        public string? ItemImageUrl { get; set; }

        public string? ItemBarcode { get; set; }


        public int ReorderLevel { get; set; }
        public int SafetyStock { get; set; }
        public int CurrentBalance { get; set; }

    }
}

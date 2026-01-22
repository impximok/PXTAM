using System;
using System.Collections.Generic;

namespace Invexaaa.Models.ViewModels
{
    public class ItemDetailViewModel
    {
        // Item
        public int ItemID { get; set; }
        public string ItemName { get; set; }
        public string CategoryName { get; set; }
        public string SupplierName { get; set; }

        public string UnitOfMeasure { get; set; }
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }

        public int ReorderLevel { get; set; }
        public int SafetyStock { get; set; }

        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? ImageUrl { get; set; }
        public string? ItemBarcode { get; set; }


        public int CurrentBalance { get; set; }
        public List<ItemBatchViewModel> Batches { get; set; }

    }
}

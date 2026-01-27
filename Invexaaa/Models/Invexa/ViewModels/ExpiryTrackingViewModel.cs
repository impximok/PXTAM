using System;

namespace Invexaaa.Models.ViewModels
{
    public class ExpiryTrackingViewModel
    {
        public int BatchID { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public int CategoryID { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public string BatchNumber { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public DateTime ExpiryDate { get; set; }

        public string ExpiryStatus { get; set; } = "Safe";

        // 🔑 REQUIRED for inactive rows
        public string ItemStatus { get; set; } = "Active";
    }
}

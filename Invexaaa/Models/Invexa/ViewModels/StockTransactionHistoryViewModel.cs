using System;

namespace Invexaaa.Models.ViewModels
{
    public class StockTransactionHistoryViewModel
    {
        public DateTime TransactionDate { get; set; }

        public string ItemName { get; set; } = "";
        public string BatchNumber { get; set; } = "";

        public string TransactionType { get; set; } = "";
        public int TransactionQuantity { get; set; }

        public decimal UnitCost { get; set; }   // 🔥 COST SHOWN
        public decimal TotalCost => UnitCost * TransactionQuantity;

        // ===== IN =====
        public string? SupplierName { get; set; }

        // ===== OUT =====
        public string? CustomerName { get; set; }

        public string? TransactionRemark { get; set; }
        public string UserName { get; set; } = "";
    }

}

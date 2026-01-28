using System;

namespace Invexaaa.Models.ViewModels
{
    public class StockTransactionHistoryViewModel
    {
        public DateTime TransactionDate { get; set; }
        public string ItemName { get; set; } = "";
        public string? BatchNumber { get; set; }
        public string TransactionType { get; set; } = "";
        public int TransactionQuantity { get; set; }
        public string TransactionRemark { get; set; } = "";
        public string UserName { get; set; } = "";
    }
}

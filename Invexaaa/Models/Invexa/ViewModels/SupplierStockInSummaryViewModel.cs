namespace Invexaaa.Models.Invexa.ViewModels
{
    public class SupplierStockInSummaryViewModel
    {
        public int SupplierID { get; set; }
        public string SupplierName { get; set; } = "";

        public int TotalQuantityReceived { get; set; }
        public decimal TotalPayableValue { get; set; }

        public decimal AverageUnitCost =>
            TotalQuantityReceived == 0
                ? 0
                : TotalPayableValue / TotalQuantityReceived;

        public int AverageLeadTimeDays { get; set; }
        public DateTime? LastDeliveryDate { get; set; }
    }
}

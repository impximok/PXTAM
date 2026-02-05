namespace Invexaaa.Models.Invexa.ViewModels
{
    public class SupplierContributionViewModel
    {
        public string SupplierName { get; set; } = "";
        public int TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
        public decimal AverageUnitCost { get; set; }
    }

}

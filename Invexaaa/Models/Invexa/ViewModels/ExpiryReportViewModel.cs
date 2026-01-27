namespace Invexaaa.Models.Invexa.ViewModels
{
    public class ExpiryReportViewModel
    {
        public string BatchNumber { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ItemStatus { get; set; } = "Active";
        public int Quantity { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}

namespace Invexaaa.Models.Invexa.ViewModels
{
    public class DemandForecastReportViewModel
    {
        public int ItemID { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string ItemStatus { get; set; } = "Active";
        public string Period { get; set; } = string.Empty;
        public int ForecastQty { get; set; }
        public int RecommendedReorder { get; set; }
    }
}

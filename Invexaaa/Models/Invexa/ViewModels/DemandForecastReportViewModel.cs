namespace Invexaaa.Models.Invexa.ViewModels
{
    public class DemandForecastReportViewModel
    {
        public int ItemID { get; set; }
        public string ItemName { get; set; } = "";
        public string ItemStatus { get; set; } = "";
        public string Period { get; set; } = "";

        public int ForecastQty { get; set; }
        public int RecommendedReorder { get; set; }

        // ✅ NEW
        public decimal AverageDailyDemand { get; set; }
    }

}

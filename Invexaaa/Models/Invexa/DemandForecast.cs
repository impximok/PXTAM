using System;
using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.Invexa
{
    public class DemandForecast
    {
        [Key]
        public int ForecastID { get; set; }

        [Required(ErrorMessage = "Item is required for demand forecasting.")]
        public int ItemID { get; set; }

        [Required(ErrorMessage = "Forecast period is required.")]
        [MaxLength(50, ErrorMessage = "Forecast period must not exceed 50 characters.")]
        public string DemandForecastPeriod { get; set; } = string.Empty;

        [Required(ErrorMessage = "Predicted quantity is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Predicted quantity cannot be negative.")]
        public int DemandPredictedQuantity { get; set; }

        [Required(ErrorMessage = "Recommended reorder quantity is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Reorder quantity cannot be negative.")]
        public int DemandRecommendedReorderQty { get; set; }

        [Required(ErrorMessage = "Forecast generation date is required.")]
        public DateTime DemandGeneratedDate { get; set; } = DateTime.Now;
    }
}

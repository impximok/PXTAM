using System;
using System.Collections.Generic;
using Invexaaa.Models.Invexa;

namespace Invexaaa.Models.ViewModels
{
    public class ReportPrintViewModel
    {
        // COMMON
        public string ReportType { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // SALES
        public List<SalesHeader>? SalesHeaders { get; set; }
        public List<SalesDetail>? SalesDetails { get; set; }

        // DEMAND FORECAST
        public List<DemandForecast>? DemandForecasts { get; set; }

        // STOCK
        public List<Inventory>? Inventories { get; set; }
        public List<StockBatch>? StockBatches { get; set; }

   

    }
}

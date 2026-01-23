using System.Collections.Generic;
using Invexaaa.Models.Invexa;
using Invexaaa.Models.ViewModels;

namespace Invexaaa.Models.ViewModels
{
    public class AlertsDashboardViewModel
    {
        public List<ExpiryTrackingViewModel> ExpiredItems { get; set; } = new();
        public List<ExpiryTrackingViewModel> NearExpiryItems { get; set; } = new();

        public List<InventoryOverviewViewModel> LowStockItems { get; set; } = new();
        public List<ItemCardViewModel> ReorderItems { get; set; } = new();
    }
}

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.ViewModels
{
    public class BulkMinusStockViewModel
    {
        public List<int> InventoryIds { get; set; } = new();

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
        public int QuantityToDeduct { get; set; }

        [Required]
        public string Reason { get; set; } = "";

        public List<BulkMinusPreviewRow> PreviewItems { get; set; } = new();
    }

    public class BulkMinusPreviewRow
    {
        public int InventoryID { get; set; }
        public string ItemName { get; set; } = "";
        public int AvailableQuantity { get; set; }
    }
}

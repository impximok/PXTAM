using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.ViewModels
{
    public class AddStockBatchViewModel
    {
        // =========================
        // INPUTS
        // =========================
        public List<int> InventoryIds { get; set; } = new();

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Expiry date is required")]
        public DateTime? ExpiryDate { get; set; }

        // =========================
        // LIVE PREVIEW (BEFORE SAVE)
        // =========================
        public List<AddStockPreviewItem> PreviewItems { get; set; } = new();

        // =========================
        // RESULT SUMMARY (AFTER SAVE)
        // =========================
        public bool ShowSummary { get; set; } = false;

        public List<AddStockBatchSummaryRow> SummaryRows { get; set; } = new();
    }

    // 🔵 Live preview row
    public class AddStockPreviewItem
    {
        public int InventoryID { get; set; }
        public string ItemName { get; set; } = "";
    }

    // 🟢 After-save summary row
    public class AddStockBatchSummaryRow
    {
        public string ItemName { get; set; } = "";
        public int QuantityAdded { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}

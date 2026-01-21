using System;
using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.Invexa
{
    public class StockAdjustment
    {
        [Key]
        public int AdjustmentID { get; set; }

        [Required(ErrorMessage = "Adjustment date is required.")]
        public DateTime AdjustmentDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Adjustment reason is required.")]
        [MaxLength(255, ErrorMessage = "Adjustment reason must not exceed 255 characters.")]
        public string AdjustmentReason { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adjustment status is required.")]
        [MaxLength(20, ErrorMessage = "Adjustment status must not exceed 20 characters.")]
        public string AdjustmentStatus { get; set; } = "Pending";
        // Pending / Approved / Rejected

        [Required(ErrorMessage = "Created by user is required.")]
        public int CreatedByUserID { get; set; }

        public int? ApprovedByUserID { get; set; }

        public DateTime? ApprovedDate { get; set; }
    }
}

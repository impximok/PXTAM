using System;
using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.Invexa
{
    public class Alert
    {
        [Key]
        public int AlertID { get; set; }

        [Required(ErrorMessage = "Item is required for the alert.")]
        public int ItemID { get; set; }

        // Optional: only applicable for batch-related alerts
        public int? BatchID { get; set; }

        [Required(ErrorMessage = "Alert type is required.")]
        public int AlertTypeID { get; set; }

        [Required(ErrorMessage = "Alert message is required.")]
        [MaxLength(500, ErrorMessage = "Alert message cannot exceed 500 characters.")]
        public string AlertMessage { get; set; } = string.Empty;

        [Required(ErrorMessage = "Alert priority is required.")]
        [MaxLength(20, ErrorMessage = "Alert priority must not exceed 20 characters.")]
        public string AlertPriority { get; set; } = "Medium";
        // Example: High / Medium / Low

        [Required(ErrorMessage = "Alert status is required.")]
        [MaxLength(20, ErrorMessage = "Alert status must not exceed 20 characters.")]
        public string AlertStatus { get; set; } = "New";
        // Example: New / Acknowledged / Resolved

        [Required(ErrorMessage = "Alert created date is required.")]
        public DateTime AlertCreatedDate { get; set; } = DateTime.Now;
    }
}

using System;
using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.Invexa
{
    public class Notification
    {
        [Key]
        public int NotificationID { get; set; }

        [Required(ErrorMessage = "Alert is required.")]
        public int AlertID { get; set; }

        [Required(ErrorMessage = "User is required.")]
        public int UserID { get; set; }

        [Required(ErrorMessage = "Notification channel is required.")]
        [MaxLength(50, ErrorMessage = "Notification channel must not exceed 50 characters.")]
        public string NotificationChannel { get; set; } = "System";

        [Required(ErrorMessage = "Notification content is required.")]
        [MaxLength(500, ErrorMessage = "Notification content must not exceed 500 characters.")]
        public string NotificationContent { get; set; } = string.Empty;

        [Required(ErrorMessage = "Notification status is required.")]
        [MaxLength(20, ErrorMessage = "Notification status must not exceed 20 characters.")]
        public string SentStatus { get; set; } = "Pending";

        public DateTime? SentDate { get; set; }
    }
}

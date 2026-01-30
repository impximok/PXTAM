using System;
using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.Invexa
{
    public class Customer
    {
        [Key]
        public int CustomerID { get; set; }

        [Required]
        [MaxLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? CustomerPhone { get; set; }

        [MaxLength(100)]
        public string? CustomerEmail { get; set; }

        [MaxLength(255)]
        public string? CustomerAddress { get; set; }

        [Required]
        [MaxLength(20)]
        public string CustomerStatus { get; set; } = "Active";

        public DateTime CustomerCreatedAt { get; set; } = DateTime.Now;
    }
}

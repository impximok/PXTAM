using System;
using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.Invexa
{
    public class SalesHeader
    {
        [Key]
        public int SalesID { get; set; }

        [Required(ErrorMessage = "Created by user is required.")]
        public int CreatedByUserID { get; set; }

        [Required(ErrorMessage = "Sales date is required.")]
        public DateTime SalesDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Total amount is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Total amount must be greater than zero.")]
        public decimal TotalAmount { get; set; }
    }
}

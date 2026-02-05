using System;
using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.Invexa
{
    public class Customer
    {
        [Key]
        public int CustomerID { get; set; }

        [Required(ErrorMessage = "Customer name is required.")]
        [MaxLength(100, ErrorMessage = "Customer name cannot exceed 100 characters.")]
        public string CustomerName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [MaxLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        public string? CustomerPhone { get; set; }

        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        public string? CustomerEmail { get; set; }

        [MaxLength(255, ErrorMessage = "Address cannot exceed 255 characters.")]
        public string? CustomerAddress { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        [MaxLength(20)]
        public string CustomerStatus { get; set; } = string.Empty;


        public DateTime CustomerCreatedAt { get; set; } = DateTime.Now;
    }
}

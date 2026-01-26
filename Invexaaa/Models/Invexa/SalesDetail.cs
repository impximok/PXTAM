using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.Invexa
{
    public class SalesDetail
    {
        [Key]
        public int SalesDetailID { get; set; }

        [Required(ErrorMessage = "Sales record is required.")]
        public int SalesID { get; set; }

        [Required(ErrorMessage = "Item is required.")]
        public int ItemID { get; set; }

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Unit price is required.")]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335",
            ErrorMessage = "Unit price must be greater than zero.")]
        public decimal UnitPrice { get; set; }

        [Required(ErrorMessage = "Subtotal is required.")]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335",
            ErrorMessage = "Subtotal must be greater than zero.")]
        public decimal Subtotal { get; set; }
    }
}

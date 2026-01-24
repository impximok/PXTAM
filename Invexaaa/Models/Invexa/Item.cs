using System;
using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.Invexa
{
    public class Item
    {
        [Key]
        public int ItemID { get; set; }

        // Dropdowns usually default to 0 -> enforce "must pick something"
        [Range(1, int.MaxValue, ErrorMessage = "Category is required.")]
        public int CategoryID { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Supplier is required.")]
        public int SupplierID { get; set; }

        [Required(ErrorMessage = "Item name is required.")]
        [StringLength(100, ErrorMessage = "Item name must not exceed 100 characters.")]
        public string ItemName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Unit of measure is required.")]
        [StringLength(20, ErrorMessage = "Unit of measure must not exceed 20 characters.")]
        public string ItemUnitOfMeasure { get; set; } = "Unit";

        [Required(ErrorMessage = "Buy price is required.")]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335",
            ErrorMessage = "Buy price must be greater than zero.")]
        public decimal ItemBuyPrice { get; set; }

        [Required(ErrorMessage = "Sell price is required.")]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335",
            ErrorMessage = "Sell price must be greater than zero.")]
        public decimal ItemSellPrice { get; set; }

        [Required(ErrorMessage = "Reorder level is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Reorder level cannot be negative.")]
        public int ItemReorderLevel { get; set; }

        [Required(ErrorMessage = "Safety stock is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Safety stock cannot be negative.")]
        public int SafetyStock { get; set; }

        [Required(ErrorMessage = "Reorder point is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Reorder point cannot be negative.")]
        public int ReorderPoint { get; set; }

        // If you want it required, add [Required]. If optional, keep only Range.
        [Range(typeof(decimal), "0", "79228162514264337593543950335",
            ErrorMessage = "Average daily demand cannot be negative.")]
        public decimal AverageDailyDemand { get; set; }

        // Optional barcode, but validate format/length if provided
        [StringLength(50, ErrorMessage = "Barcode must not exceed 50 characters.")]
        [RegularExpression(@"^[0-9A-Za-z\-]*$", ErrorMessage = "Barcode can only contain letters, numbers, and '-'.")]
        public string? ItemBarcode { get; set; }

        [StringLength(255, ErrorMessage = "Image URL must not exceed 255 characters.")]
        public string? ItemImageUrl { get; set; }

        [Required(ErrorMessage = "Item status is required.")]
        [StringLength(20, ErrorMessage = "Item status must not exceed 20 characters.")]
        [RegularExpression(@"^(Active|Inactive)$", ErrorMessage = "Item status must be Active or Inactive.")]
        public string ItemStatus { get; set; } = "Active";

        // You usually DON'T need [Required] for DateTime (it's non-nullable anyway).
        // Keep the default.
        public DateTime ItemCreatedDate { get; set; } = DateTime.Now;
    }
}

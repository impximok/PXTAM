using System;
using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.Invexa
{
    public class Item
    {
        [Key]
        public int ItemID { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "Supplier is required.")]
        public int SupplierID { get; set; }

        [Required(ErrorMessage = "Item name is required.")]
        [MaxLength(100, ErrorMessage = "Item name must not exceed 100 characters.")]
        public string ItemName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Unit of measure is required.")]
        [MaxLength(20, ErrorMessage = "Unit of measure must not exceed 20 characters.")]
        public string ItemUnitOfMeasure { get; set; } = "Unit";

        [Required(ErrorMessage = "Reorder level is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Reorder level cannot be negative.")]
        public int ItemReorderLevel { get; set; }

        [Required(ErrorMessage = "Safety stock is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Safety stock cannot be negative.")]
        public int SafetyStock { get; set; }

        [Required(ErrorMessage = "Reorder point is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Reorder point cannot be negative.")]
        public int ReorderPoint { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Average daily demand cannot be negative.")]
        public decimal AverageDailyDemand { get; set; }

        [MaxLength(50, ErrorMessage = "Barcode must not exceed 50 characters.")]
        public string? ItemBarcode { get; set; }

        [Required(ErrorMessage = "Buy price is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Buy price must be greater than zero.")]
        public decimal ItemBuyPrice { get; set; }

        [Required(ErrorMessage = "Sell price is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Sell price must be greater than zero.")]
        public decimal ItemSellPrice { get; set; }

        [MaxLength(255, ErrorMessage = "Image URL must not exceed 255 characters.")]
        public string? ItemImageUrl { get; set; }

        [Required(ErrorMessage = "Item status is required.")]
        [MaxLength(20, ErrorMessage = "Item status must not exceed 20 characters.")]
        public string ItemStatus { get; set; } = "Active";

        [Required(ErrorMessage = "Item created date is required.")]
        public DateTime ItemCreatedDate { get; set; } = DateTime.Now;
    }
}

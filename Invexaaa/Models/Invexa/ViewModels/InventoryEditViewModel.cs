using System;
using System.ComponentModel.DataAnnotations;
using Invexaaa.Models.Invexa.Enums;

namespace Invexaaa.Models.ViewModels
{
    public class InventoryEditViewModel
    {
        public int InventoryID { get; set; }
        public int ItemID { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public int CurrentQuantity { get; set; }

        public CostingMethod CostingMethod { get; set; }

        [Required]
        [Range(0.0001, double.MaxValue, ErrorMessage = "Standard cost must be greater than zero.")]
        public decimal StandardUnitCost { get; set; }

        public decimal TotalStockValue { get; set; }

        public DateTime? LastCostUpdated { get; set; }
        public int InventoryTotalQuantity { get; set; }

    }
}

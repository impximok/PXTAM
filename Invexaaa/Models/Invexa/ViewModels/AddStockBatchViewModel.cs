using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.ViewModels
{
    public class AddStockBatchViewModel
    {
        [Required]
        public List<int> InventoryIds { get; set; } = new();

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }
    }
}

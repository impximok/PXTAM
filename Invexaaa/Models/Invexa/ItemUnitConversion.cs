using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.Invexa
{
    public class ItemUnitConversion
    {
        [Key]
        public int ItemUnitConversionID { get; set; }

        // 🔗 Which item this unit belongs to
        [Required]
        public int ItemID { get; set; }

        // e.g. "unit", "pack", "carton"
        [Required]
        [MaxLength(50)]
        public string UnitName { get; set; } = string.Empty;

        // How many BASE units this represents
        // Example:
        // pack   → 2
        // carton → 28
        [Range(1, int.MaxValue)]
        public int BaseUnitMultiplier { get; set; }

        // TRUE only for the base unit (e.g. "pencil")
        public bool IsBaseUnit { get; set; } = false;
    }
}

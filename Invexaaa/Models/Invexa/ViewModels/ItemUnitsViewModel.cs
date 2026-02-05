using System.Collections.Generic;
using Invexaaa.Models.Invexa;
using Invexaaa.Models.Invexa.Enums;

namespace Invexaaa.Models.Invexa.ViewModels
{
    public class ItemUnitsViewModel
    {
        // =========================
        // ITEM CONTEXT
        // =========================
        public int ItemID { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public CostingMethod CostingMethod { get; set; }

        // =========================
        // SAFETY FLAGS
        // =========================
        // TRUE if any stock / batch exists → lock deletion
        public bool HasStock { get; set; }

        // =========================
        // EXISTING UNITS
        // =========================
        public List<ItemUnitConversion> Units { get; set; }
            = new List<ItemUnitConversion>();

        // =========================
        // ADD UNIT FORM FIELDS
        // =========================
        public string NewUnitName { get; set; } = string.Empty;

        // How many base units this represents
        // e.g. kg → 1000 (g)
        public int NewBaseUnitMultiplier { get; set; }

        // Only ONE base unit allowed
        public bool NewIsBaseUnit { get; set; }
    }
}

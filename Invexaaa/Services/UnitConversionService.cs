using Invexaaa.Data;
using Microsoft.EntityFrameworkCore;

namespace Invexaaa.Services
{
    public class UnitConversionService
    {
        private readonly InvexaDbContext _context;

        public UnitConversionService(InvexaDbContext context)
        {
            _context = context;
        }

        public void ValidateBaseUnit(int itemId)
        {
            var baseUnits = _context.ItemUnitConversions
                .Where(u => u.ItemID == itemId && u.IsBaseUnit)
                .Count();

            if (baseUnits != 1)
            {
                throw new InvalidOperationException(
                    "Each item must have exactly ONE base unit."
                );
            }
        }

        public int ConvertToBaseUnit(int itemId, string unitName, int quantity)
        {
            var unit = _context.ItemUnitConversions
                .FirstOrDefault(u =>
                    u.ItemID == itemId &&
                    u.UnitName == unitName);

            if (unit == null)
                throw new InvalidOperationException("Invalid unit selected.");

            return quantity * unit.BaseUnitMultiplier;
        }
    }
}

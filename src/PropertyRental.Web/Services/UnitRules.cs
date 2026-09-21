using System.Linq.Expressions;
using PropertyRental.Web.Models;

namespace PropertyRental.Web.Services;

public static class UnitRules
{
    public static bool CanAssignType(UnitType type, int? currentTypeId) => type.IsActive || type.Id == currentTypeId;

    // An expression rather than a compiled delegate keeps filtering in SQL.
    public static Expression<Func<Unit, bool>> AvailableOn(DateOnly date) =>
        unit => !unit.Leases.Any(lease => lease.StartDate <= date && lease.EndDate >= date);
}

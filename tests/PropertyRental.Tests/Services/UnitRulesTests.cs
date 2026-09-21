using PropertyRental.Web.Models;
using PropertyRental.Web.Services;

namespace PropertyRental.Tests.Services;

public class UnitRulesTests
{
    [Theory]
    [InlineData(true, null, true)]
    [InlineData(true, 1, true)]
    [InlineData(true, 2, true)]
    [InlineData(false, null, false)]
    [InlineData(false, 2, false)]
    [InlineData(false, 1, true)]
    public void Inactive_type_can_only_be_retained_by_its_current_unit(bool active, int? currentTypeId, bool allowed)
    {
        var type = new UnitType { Id = 1, Name = "Legacy", IsActive = active };
        Assert.Equal(allowed, UnitRules.CanAssignType(type, currentTypeId));
    }

    [Theory]
    [InlineData(-5, 5, false)]
    [InlineData(0, 5, false)]
    [InlineData(-5, 0, false)]
    [InlineData(0, 0, false)]
    [InlineData(-5, -1, true)]
    [InlineData(1, 5, true)]
    public void Availability_uses_inclusive_start_and_end_dates(int startOffset, int endOffset, bool available)
    {
        var today = new DateOnly(2026, 10, 1);
        var unit = new Unit { UnitNumber = "101" };
        unit.Leases.Add(new Lease { StartDate = today.AddDays(startOffset), EndDate = today.AddDays(endOffset) });
        Assert.Equal(available, UnitRules.AvailableOn(today).Compile()(unit));
    }

    [Fact]
    public void Unit_without_leases_is_available()
    {
        Assert.True(UnitRules.AvailableOn(new(2026, 10, 1)).Compile()(new Unit { UnitNumber = "101" }));
    }

    [Fact]
    public void Any_active_lease_blocks_unit_even_with_expired_and_future_leases()
    {
        var today = new DateOnly(2026, 10, 1);
        var unit = new Unit { UnitNumber = "101" };
        unit.Leases.Add(new Lease { StartDate = today.AddYears(-2), EndDate = today.AddYears(-1) });
        unit.Leases.Add(new Lease { StartDate = today.AddYears(1), EndDate = today.AddYears(2) });
        unit.Leases.Add(new Lease { StartDate = today, EndDate = today.AddMonths(12).AddDays(-1) });
        Assert.False(UnitRules.AvailableOn(today).Compile()(unit));
    }
}

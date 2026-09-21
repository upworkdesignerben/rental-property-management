using Microsoft.EntityFrameworkCore;
using PropertyRental.Web.Data;
using PropertyRental.Web.Models;
using PropertyRental.Web.Services.Interfaces;
using PropertyRental.Web.ViewModels.Units;

namespace PropertyRental.Web.Services;

public class UnitService(ApplicationDbContext dbContext, TimeProvider timeProvider) : IUnitService
{
    public async Task<UnitListViewModel> GetManagerListAsync(int? propertyId, CancellationToken cancellationToken = default)
    {
        var today = Today;
        var unitsQuery = dbContext.Units.AsNoTracking();
        if (propertyId.HasValue)
        {
            unitsQuery = unitsQuery.Where(unit => unit.PropertyId == propertyId.Value);
        }

        var units = await unitsQuery
            .OrderBy(unit => unit.Property.Name)
            .ThenBy(unit => unit.UnitNumber)
            .Select(unit => new UnitListItemViewModel
            {
                Id = unit.Id,
                PropertyId = unit.PropertyId,
                PropertyName = unit.Property.Name,
                UnitNumber = unit.UnitNumber,
                UnitTypeName = unit.UnitType.Name,
                Bedrooms = unit.Bedrooms,
                MonthlyRent = unit.MonthlyRent,
                IsAvailable = !unit.Leases.Any(lease => lease.StartDate <= today && lease.EndDate >= today)
            })
            .ToListAsync(cancellationToken);

        return new UnitListViewModel
        {
            PropertyId = propertyId,
            Properties = await GetPropertyOptionsAsync(cancellationToken),
            Units = units
        };
    }

    public async Task<UnitListViewModel> GetAvailableListAsync(CancellationToken cancellationToken = default)
    {
        var today = Today;
        var units = await dbContext.Units
            .AsNoTracking()
            .Where(UnitRules.AvailableOn(today))
            .OrderBy(unit => unit.Property.Name)
            .ThenBy(unit => unit.UnitNumber)
            .Select(unit => new UnitListItemViewModel
            {
                Id = unit.Id,
                PropertyId = unit.PropertyId,
                PropertyName = unit.Property.Name,
                UnitNumber = unit.UnitNumber,
                UnitTypeName = unit.UnitType.Name,
                Bedrooms = unit.Bedrooms,
                MonthlyRent = unit.MonthlyRent,
                IsAvailable = true
            })
            .ToListAsync(cancellationToken);

        return new UnitListViewModel { Properties = [], Units = units };
    }

    public async Task<UnitFormViewModel> GetCreateFormAsync(CancellationToken cancellationToken = default) =>
        await PopulateOptionsAsync(new UnitFormViewModel(), cancellationToken);

    public async Task<UnitFormViewModel?> GetEditFormAsync(int id, CancellationToken cancellationToken = default)
    {
        var model = await dbContext.Units
            .AsNoTracking()
            .Where(unit => unit.Id == id)
            .Select(unit => new UnitFormViewModel
            {
                Id = unit.Id,
                PropertyId = unit.PropertyId,
                UnitTypeId = unit.UnitTypeId,
                UnitNumber = unit.UnitNumber,
                Bedrooms = unit.Bedrooms,
                MonthlyRent = unit.MonthlyRent
            })
            .SingleOrDefaultAsync(cancellationToken);

        return model is null ? null : await PopulateOptionsAsync(model, cancellationToken);
    }

    public async Task<UnitFormViewModel> PopulateOptionsAsync(UnitFormViewModel model, CancellationToken cancellationToken = default)
    {
        var currentTypeId = model.Id.HasValue
            ? await dbContext.Units.Where(unit => unit.Id == model.Id).Select(unit => (int?)unit.UnitTypeId).SingleOrDefaultAsync(cancellationToken)
            : null;
        var unitTypes = await dbContext.UnitTypes
            .AsNoTracking()
            .Where(unitType => unitType.IsActive || unitType.Id == currentTypeId)
            .OrderBy(unitType => unitType.Name)
            .Select(unitType => new UnitOptionViewModel(unitType.Id, unitType.Name))
            .ToListAsync(cancellationToken);

        return model with
        {
            Properties = await GetPropertyOptionsAsync(cancellationToken),
            UnitTypes = unitTypes
        };
    }

    public async Task<UnitSaveResult> CreateAsync(UnitFormViewModel model, CancellationToken cancellationToken = default)
    {
        var validationError = await ValidateAsync(model, null, cancellationToken);
        if (validationError is not null)
        {
            return new UnitSaveResult(false, validationError);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        dbContext.Units.Add(new Unit
        {
            PropertyId = model.PropertyId,
            UnitTypeId = model.UnitTypeId,
            UnitNumber = model.UnitNumber.Trim(),
            Bedrooms = model.Bedrooms,
            MonthlyRent = model.MonthlyRent,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return new UnitSaveResult(true);
    }

    public async Task<UnitSaveResult> UpdateAsync(int id, UnitFormViewModel model, CancellationToken cancellationToken = default)
    {
        var unit = await dbContext.Units.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (unit is null)
        {
            return new UnitSaveResult(false, Found: false);
        }

        var validationError = await ValidateAsync(model, unit.UnitTypeId, cancellationToken, id);
        if (validationError is not null)
        {
            return new UnitSaveResult(false, validationError);
        }

        unit.PropertyId = model.PropertyId;
        unit.UnitTypeId = model.UnitTypeId;
        unit.UnitNumber = model.UnitNumber.Trim();
        unit.Bedrooms = model.Bedrooms;
        unit.MonthlyRent = model.MonthlyRent;
        unit.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new UnitSaveResult(true);
    }

    public async Task<UnitDeleteResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var unit = await dbContext.Units
            .Include(item => item.RentalApplications)
            .Include(item => item.Leases)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (unit is null)
        {
            return new UnitDeleteResult(false, false);
        }

        if (unit.RentalApplications.Count != 0 || unit.Leases.Count != 0)
        {
            return new UnitDeleteResult(true, true);
        }

        dbContext.Units.Remove(unit);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new UnitDeleteResult(true, false);
    }

    public async Task<UnitDetailsViewModel?> GetAvailableDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var today = Today;
        return await dbContext.Units
            .AsNoTracking()
            .Where(unit => unit.Id == id)
            .Where(UnitRules.AvailableOn(today))
            .Select(unit => new UnitDetailsViewModel
            {
                Id = unit.Id,
                PropertyName = unit.Property.Name,
                PropertyAddress = unit.Property.Address,
                UnitNumber = unit.UnitNumber,
                UnitTypeName = unit.UnitType.Name,
                Bedrooms = unit.Bedrooms,
                MonthlyRent = unit.MonthlyRent
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> IsAvailableAsync(int unitId, DateOnly date, CancellationToken cancellationToken = default) =>
        dbContext.Units.Where(UnitRules.AvailableOn(date)).AnyAsync(unit => unit.Id == unitId, cancellationToken);

    private DateOnly Today => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

    private Task<List<UnitOptionViewModel>> GetPropertyOptionsAsync(CancellationToken cancellationToken) =>
        dbContext.Properties
            .AsNoTracking()
            .OrderBy(property => property.Name)
            .Select(property => new UnitOptionViewModel(property.Id, property.Name))
            .ToListAsync(cancellationToken);

    private async Task<string?> ValidateAsync(
        UnitFormViewModel model,
        int? currentUnitTypeId,
        CancellationToken cancellationToken,
        int? currentUnitId = null)
    {
        if (!await dbContext.Properties.AnyAsync(property => property.Id == model.PropertyId, cancellationToken))
        {
            return "The selected property no longer exists.";
        }

        var unitType = await dbContext.UnitTypes.SingleOrDefaultAsync(type => type.Id == model.UnitTypeId, cancellationToken);
        if (unitType is null)
        {
            return "The selected unit type no longer exists.";
        }

        if (!UnitRules.CanAssignType(unitType, currentUnitTypeId))
        {
            return "Only active unit types can be selected.";
        }

        var unitNumber = model.UnitNumber.Trim();
        if (await dbContext.Units.AnyAsync(
                unit => unit.PropertyId == model.PropertyId && unit.UnitNumber == unitNumber && unit.Id != currentUnitId,
                cancellationToken))
        {
            return "A unit with this number already exists at the selected property.";
        }

        return null;
    }
}

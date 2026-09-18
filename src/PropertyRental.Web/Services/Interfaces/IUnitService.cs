using PropertyRental.Web.ViewModels.Units;

namespace PropertyRental.Web.Services.Interfaces;

public interface IUnitService
{
    Task<UnitListViewModel> GetManagerListAsync(int? propertyId, CancellationToken cancellationToken = default);

    Task<UnitListViewModel> GetAvailableListAsync(CancellationToken cancellationToken = default);

    Task<UnitFormViewModel> GetCreateFormAsync(CancellationToken cancellationToken = default);

    Task<UnitFormViewModel?> GetEditFormAsync(int id, CancellationToken cancellationToken = default);

    Task<UnitFormViewModel> PopulateOptionsAsync(UnitFormViewModel model, CancellationToken cancellationToken = default);

    Task<UnitSaveResult> CreateAsync(UnitFormViewModel model, CancellationToken cancellationToken = default);

    Task<UnitSaveResult> UpdateAsync(int id, UnitFormViewModel model, CancellationToken cancellationToken = default);

    Task<UnitDeleteResult> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<UnitDetailsViewModel?> GetAvailableDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> IsAvailableAsync(int unitId, DateOnly date, CancellationToken cancellationToken = default);
}

public record UnitSaveResult(bool Succeeded, string? Error = null, bool Found = true);

public record UnitDeleteResult(bool Found, bool HasDependencies);

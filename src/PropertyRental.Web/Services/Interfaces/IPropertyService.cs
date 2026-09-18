using PropertyRental.Web.ViewModels.Properties;

namespace PropertyRental.Web.Services.Interfaces;

public interface IPropertyService
{
    Task<PropertyListViewModel> GetListAsync(CancellationToken cancellationToken = default);

    Task<PropertyFormViewModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default);

    Task CreateAsync(PropertyFormViewModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(int id, PropertyFormViewModel model, CancellationToken cancellationToken = default);

    Task<PropertyDeleteResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public record PropertyDeleteResult(bool Found, bool HasUnits);

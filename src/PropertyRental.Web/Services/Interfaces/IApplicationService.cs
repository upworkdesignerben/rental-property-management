using PropertyRental.Web.ViewModels.Applications;

namespace PropertyRental.Web.Services.Interfaces;

public partial interface IApplicationService
{
    Task<ApplicationListViewModel> GetListAsync(
        string userId,
        bool isManager,
        ApplicationListFilterViewModel filter,
        CancellationToken cancellationToken = default);

    Task<ApplicationDetailsViewModel?> GetDetailsAsync(
        int id,
        string userId,
        bool isManager,
        CancellationToken cancellationToken = default);
}

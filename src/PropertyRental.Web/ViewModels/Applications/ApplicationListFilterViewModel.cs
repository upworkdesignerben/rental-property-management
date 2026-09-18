using PropertyRental.Web.Models.Enums;

namespace PropertyRental.Web.ViewModels.Applications;

public class ApplicationListFilterViewModel
{
    public RentalApplicationStatus? Status { get; init; }

    public int? PropertyId { get; init; }
}

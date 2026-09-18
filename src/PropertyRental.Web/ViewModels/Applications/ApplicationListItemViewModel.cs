using PropertyRental.Web.Models.Enums;

namespace PropertyRental.Web.ViewModels.Applications;

public class ApplicationListItemViewModel
{
    public int Id { get; init; }

    public required string ApplicantName { get; init; }

    public required string PropertyName { get; init; }

    public required string UnitNumber { get; init; }

    public RentalApplicationStatus Status { get; init; }

    public DateTime UpdatedAtUtc { get; init; }
}

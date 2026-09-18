using PropertyRental.Web.Models.Enums;

namespace PropertyRental.Web.ViewModels.Applications;

public class ApplicationDetailsViewModel
{
    public int Id { get; init; }

    public bool IsManager { get; init; }

    public required string PropertyName { get; init; }

    public required string PropertyAddress { get; init; }

    public required string UnitNumber { get; init; }

    public required string UnitTypeName { get; init; }

    public int Bedrooms { get; init; }

    public decimal MonthlyRent { get; init; }

    public RentalApplicationStatus Status { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public string? ApplicantName { get; init; }

    public string? ApplicantPhone { get; init; }

    public string? ApplicantEmail { get; init; }

    public string? CurrentAddress { get; init; }

    public string? ReturnComment { get; init; }

    public DateOnly? LeaseStartDate { get; init; }

    public DateOnly? LeaseEndDate { get; init; }

    public required IReadOnlyList<ResidenceItemViewModel> Residences { get; init; }

    public required IReadOnlyList<StatusHistoryItemViewModel> StatusHistory { get; set; }

    public required IReadOnlyList<ReviewItemViewModel> Reviews { get; set; }
}

public class ResidenceItemViewModel
{
    public required string Address { get; init; }

    public required string LandlordName { get; init; }

    public required string LandlordPhone { get; init; }

    public DateOnly MoveInDate { get; init; }

    public DateOnly MoveOutDate { get; init; }
}

public class ReviewItemViewModel
{
    public required string Outcome { get; init; }

    public string? Comment { get; init; }

    public required string ReviewerEmail { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}

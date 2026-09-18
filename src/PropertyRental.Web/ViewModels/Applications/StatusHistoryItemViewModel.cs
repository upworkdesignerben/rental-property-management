using PropertyRental.Web.Models.Enums;

namespace PropertyRental.Web.ViewModels.Applications;

public class StatusHistoryItemViewModel
{
    public RentalApplicationStatus? PreviousStatus { get; init; }

    public RentalApplicationStatus NewStatus { get; init; }

    public required string ChangedBy { get; init; }

    public string? Comment { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}

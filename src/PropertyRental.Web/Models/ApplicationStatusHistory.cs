using PropertyRental.Web.Models.Enums;

namespace PropertyRental.Web.Models;

public class ApplicationStatusHistory
{
    public int Id { get; set; }

    public int RentalApplicationId { get; set; }

    public RentalApplicationStatus? PreviousStatus { get; set; }

    public RentalApplicationStatus NewStatus { get; set; }

    public required string ChangedByUserId { get; set; }

    public ReviewOutcome? ReviewOutcome { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public RentalApplication RentalApplication { get; set; } = null!;

    public ApplicationUser ChangedByUser { get; set; } = null!;
}

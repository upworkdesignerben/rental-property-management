using PropertyRental.Web.Models.Enums;

namespace PropertyRental.Web.Models;

public class ApplicationReview
{
    public int Id { get; set; }

    public int RentalApplicationId { get; set; }

    public required string ReviewerId { get; set; }

    public ReviewOutcome Outcome { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public RentalApplication RentalApplication { get; set; } = null!;

    public ApplicationUser Reviewer { get; set; } = null!;
}

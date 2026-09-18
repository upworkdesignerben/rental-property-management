namespace PropertyRental.Web.Models;

public class ApplicantInformation
{
    public int RentalApplicationId { get; set; }

    public required string Name { get; set; }

    public required string Phone { get; set; }

    public required string Email { get; set; }

    public required string CurrentAddress { get; set; }

    public RentalApplication RentalApplication { get; set; } = null!;
}

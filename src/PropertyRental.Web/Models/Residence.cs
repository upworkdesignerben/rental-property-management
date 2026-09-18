namespace PropertyRental.Web.Models;

public class Residence
{
    public int Id { get; set; }

    public int RentalApplicationId { get; set; }

    public required string Address { get; set; }

    public required string LandlordName { get; set; }

    public required string LandlordPhone { get; set; }

    public DateOnly MoveInDate { get; set; }

    public DateOnly MoveOutDate { get; set; }

    public RentalApplication RentalApplication { get; set; } = null!;
}

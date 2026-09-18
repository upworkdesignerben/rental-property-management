namespace PropertyRental.Web.Models;

public class Lease
{
    public int Id { get; set; }

    public int ApplicationId { get; set; }

    public int UnitId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public RentalApplication Application { get; set; } = null!;

    public Unit Unit { get; set; } = null!;
}

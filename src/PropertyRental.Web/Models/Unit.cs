namespace PropertyRental.Web.Models;

public class Unit
{
    public int Id { get; set; }

    public int PropertyId { get; set; }

    public int UnitTypeId { get; set; }

    public required string UnitNumber { get; set; }

    public int Bedrooms { get; set; }

    public decimal MonthlyRent { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Property Property { get; set; } = null!;

    public UnitType UnitType { get; set; } = null!;

    public ICollection<RentalApplication> RentalApplications { get; } = [];

    public ICollection<Lease> Leases { get; } = [];
}

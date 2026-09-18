namespace PropertyRental.Web.Models;

public class UnitType
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public bool IsActive { get; set; }

    public ICollection<Unit> Units { get; } = [];
}

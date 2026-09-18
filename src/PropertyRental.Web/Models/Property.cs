namespace PropertyRental.Web.Models;

public class Property
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Address { get; set; }

    public required string Description { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<Unit> Units { get; } = [];
}

namespace PropertyRental.Web.ViewModels.Properties;

public class PropertyListItemViewModel
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public required string Address { get; init; }

    public required string Description { get; init; }

    public int UnitCount { get; init; }
}

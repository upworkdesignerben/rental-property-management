namespace PropertyRental.Web.ViewModels.Units;

public class UnitDetailsViewModel
{
    public int Id { get; init; }

    public required string PropertyName { get; init; }

    public required string PropertyAddress { get; init; }

    public required string UnitNumber { get; init; }

    public required string UnitTypeName { get; init; }

    public int Bedrooms { get; init; }

    public decimal MonthlyRent { get; init; }
}

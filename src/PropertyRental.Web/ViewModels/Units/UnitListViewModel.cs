namespace PropertyRental.Web.ViewModels.Units;

public class UnitListViewModel
{
    public int? PropertyId { get; init; }

    public required IReadOnlyList<UnitOptionViewModel> Properties { get; init; }

    public required IReadOnlyList<UnitListItemViewModel> Units { get; init; }
}

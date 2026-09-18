namespace PropertyRental.Web.ViewModels.Properties;

public class PropertyListViewModel
{
    public required IReadOnlyList<PropertyListItemViewModel> Properties { get; init; }
}

namespace PropertyRental.Web.ViewModels.Applications;

public class ApplicationListViewModel
{
    public bool IsManager { get; init; }

    public required ApplicationListFilterViewModel Filter { get; init; }

    public required IReadOnlyList<ApplicationListItemViewModel> Applications { get; init; }

    public required IReadOnlyList<ApplicationPropertyOptionViewModel> Properties { get; init; }
}

public record ApplicationPropertyOptionViewModel(int Id, string Name);

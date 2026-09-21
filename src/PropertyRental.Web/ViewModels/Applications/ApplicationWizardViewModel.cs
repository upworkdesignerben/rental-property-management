using PropertyRental.Web.Models.Enums;

namespace PropertyRental.Web.ViewModels.Applications;

public class ApplicationWizardViewModel
{
    public int Id { get; init; }
    public string UnitSummary { get; init; } = string.Empty;
    public RentalApplicationStatus Status { get; init; }
    public ApplicationStep CurrentStep { get; init; }
    public bool IsEditable => Status is RentalApplicationStatus.Draft or RentalApplicationStatus.Returned;
    public bool ApplicantInformationSaved { get; init; }
    public bool ResidenceHistorySaved { get; init; }
    public ApplicantInformationViewModel ApplicantInformation { get; set; } = new();
    public ResidenceHistoryViewModel ResidenceHistory { get; init; } = new();
}

public class ResidenceHistoryViewModel
{
    public int ApplicationId { get; init; }
    public bool IsEditable { get; init; }
    public IReadOnlyList<ResidenceFormViewModel> Items { get; init; } = [];
}

public record ApplicationSummaryViewModel(ApplicantInformationViewModel ApplicantInformation, ResidenceHistoryViewModel ResidenceHistory);

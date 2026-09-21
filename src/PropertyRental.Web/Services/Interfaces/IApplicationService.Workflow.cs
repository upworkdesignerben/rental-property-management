using PropertyRental.Web.Models.Enums;
using PropertyRental.Web.ViewModels.Applications;
using PropertyRental.Web.ViewModels.Reviews;

namespace PropertyRental.Web.Services.Interfaces;

public partial interface IApplicationService
{
    Task<ApplicationWizardViewModel?> GetWizardAsync(int id, string userId, CancellationToken ct = default);
    Task<ApplicationMutationResult> CreateDraftAsync(int unitId, string userId, CancellationToken ct = default);
    Task<ApplicationMutationResult> WizardAsync(int id, string userId, WizardCommand command, ApplicantInformationViewModel information, CancellationToken ct = default);
    Task<ApplicationMutationResult> WithdrawAsync(int id, string userId, CancellationToken ct = default);
    Task<ApplicationMutationResult> SaveResidenceAsync(int applicationId, int? residenceId, string userId, ResidenceFormViewModel model, bool delete = false, CancellationToken ct = default);
    Task<ApplicationMutationResult> ReviewAsync(int id, string managerId, ApplicationReviewViewModel model, CancellationToken ct = default);
}

public record ApplicationMutationResult(int StatusCode = 200, int? Id = null, string? Message = null,
    IReadOnlyDictionary<string, string[]>? Errors = null)
{
    public bool Succeeded => StatusCode == 200;
}

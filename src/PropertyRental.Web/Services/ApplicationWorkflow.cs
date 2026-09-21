using System.ComponentModel.DataAnnotations;

using PropertyRental.Web.Models;
using PropertyRental.Web.Models.Enums;
using PropertyRental.Web.Services.Interfaces;
using PropertyRental.Web.ViewModels.Applications;
using PropertyRental.Web.ViewModels.Reviews;

namespace PropertyRental.Web.Services;

// Business operations on a loaded application. Persistence, SQL availability checks and
// transaction boundaries remain in ApplicationService; this class has no infrastructure dependencies.
public sealed class ApplicationWorkflow(TimeProvider timeProvider)
{
    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;
    public static bool IsEditable(RentalApplicationStatus status) => status is RentalApplicationStatus.Draft or RentalApplicationStatus.Returned;

    public ApplicationMutationResult Wizard(RentalApplication application, string userId, WizardCommand command,
        ApplicantInformationViewModel information, bool unitAvailable)
    {
        var access = CheckWizardAccess(application, userId);
        if (access is not null) return access;
        var result = command switch
        {
            WizardCommand.Back => MoveBack(application),
            WizardCommand.Continue => ContinueStep(application, information),
            WizardCommand.Submit => SubmitApplication(application, userId, unitAvailable),
            _ => new ApplicationMutationResult(400, Message: "This command is not valid for the current step.")
        };
        if (result.Succeeded) application.UpdatedAtUtc = Now;
        return result;
    }

    private static ApplicationMutationResult? CheckWizardAccess(RentalApplication application, string userId)
    {
        if (application.ApplicantId != userId) return new(404);
        if (!IsEditable(application.Status)) return new(409, Message: "This application is read-only. Reload to see its current status.");
        return null;
    }

    // Back never validates and discards posted inputs.
    private static ApplicationMutationResult MoveBack(RentalApplication application)
    {
        application.CurrentStep = application.CurrentStep == ApplicationStep.Summary
            ? ApplicationStep.ResidenceHistory
            : ApplicationStep.ApplicantInformation;
        return new(Id: application.Id);
    }

    private ApplicationMutationResult ContinueStep(RentalApplication application, ApplicantInformationViewModel information) =>
        application.CurrentStep switch
        {
            ApplicationStep.ApplicantInformation => SaveApplicantInformation(application, information),
            ApplicationStep.ResidenceHistory => SaveResidenceHistory(application),
            _ => new(400, Message: "This command is not valid for the current step.")
        };

    private ApplicationMutationResult SaveApplicantInformation(RentalApplication application, ApplicantInformationViewModel information)
    {
        var invalid = Validate(information, "ApplicantInformation.");
        if (invalid is not null) return invalid;
        application.ApplicantInformation ??= new ApplicantInformation { Name = "", Phone = "", Email = "", CurrentAddress = "" };
        application.ApplicantInformation.Name = information.Name.Trim();
        application.ApplicantInformation.Phone = information.Phone.Trim();
        application.ApplicantInformation.Email = information.Email.Trim();
        application.ApplicantInformation.CurrentAddress = information.CurrentAddress.Trim();
        application.ApplicantInformationSaved = true;
        application.CurrentStep = ApplicationStep.ResidenceHistory;
        return new(Id: application.Id);
    }

    // Residences are edited through modals, so Continue revalidates the persisted records.
    private ApplicationMutationResult SaveResidenceHistory(RentalApplication application)
    {
        if (!ValidResidences(application)) return new(409, Message: "Please correct the residence history before continuing.");
        application.ResidenceHistorySaved = true;
        application.CurrentStep = ApplicationStep.Summary;
        return new(Id: application.Id);
    }

    private ApplicationMutationResult SubmitApplication(RentalApplication application, string userId, bool unitAvailable)
    {
        if (application.CurrentStep != ApplicationStep.Summary
            || !application.ApplicantInformationSaved
            || !application.ResidenceHistorySaved)
            return new(409, Message: "Complete both sections and review the summary before submitting.");
        if (Validate(Information(application.ApplicantInformation)) is not null || !ValidResidences(application))
            return new(409, Message: "Please go back and correct the application information.");
        if (!unitAvailable) return new(409, Message: "This unit is no longer available. Your application has not been submitted.");
        Transition(application, RentalApplicationStatus.Submitted, userId);
        application.SubmittedAtUtc = Now;
        return new(Id: application.Id);
    }

    public ApplicationMutationResult Withdraw(RentalApplication application, string userId)
    {
        if (application.ApplicantId != userId) return new(404);
        if (!IsEditable(application.Status) && application.Status != RentalApplicationStatus.Submitted)
            return new(409, Message: "This application cannot be withdrawn.");
        Transition(application, RentalApplicationStatus.Withdrawn, userId);
        return new(Id: application.Id);
    }

    public ApplicationMutationResult SaveResidence(RentalApplication application, int? residenceId, string userId,
        ResidenceFormViewModel model, bool delete)
    {
        if (application.ApplicantId != userId) return new(404);
        if (!IsEditable(application.Status)) return new(409, Message: "This application is no longer editable. Reload the page.");
        var residence = residenceId.HasValue ? application.Residences.SingleOrDefault(r => r.Id == residenceId) : null;
        if (residenceId.HasValue && residence is null) return new(404);
        if (delete)
        {
            if (residence is null) return new(404);
            application.Residences.Remove(residence);
        }
        else
        {
            var invalid = Validate(model);
            if (invalid is not null) return invalid;
            if (residence is null)
            {
                residence = new Residence { RentalApplicationId = application.Id, Address = "", LandlordName = "", LandlordPhone = "" };
                application.Residences.Add(residence);
            }
            residence.Address = model.Address.Trim();
            residence.LandlordName = model.LandlordName.Trim();
            residence.LandlordPhone = model.LandlordPhone.Trim();
            residence.MoveInDate = model.MoveInDate!.Value;
            residence.MoveOutDate = model.MoveOutDate!.Value;
        }
        application.ResidenceHistorySaved = false;
        application.CurrentStep = ApplicationStep.ResidenceHistory;
        application.UpdatedAtUtc = Now;
        return new(Id: application.Id);
    }

    public ApplicationMutationResult Review(RentalApplication application, string managerId, ApplicationReviewViewModel model, bool unitAvailable)
    {
        if (application.Status != RentalApplicationStatus.Submitted) return new(409, Message: "Only submitted applications can be reviewed. Reload the page.");
        var invalid = Validate(model);
        if (invalid is not null) return invalid;
        var outcome = model.Outcome!.Value;
        if (outcome == ReviewOutcome.Approved)
        {
            if (!unitAvailable || application.Lease is not null) return new(409, Message: "The unit already has an active lease. Approval was not saved.");
            var today = DateOnly.FromDateTime(Now);
            application.Lease = new Lease { ApplicationId = application.Id, UnitId = application.UnitId, StartDate = today, EndDate = today.AddMonths(12).AddDays(-1), CreatedAtUtc = Now };
        }
        var comment = string.IsNullOrWhiteSpace(model.Comment) ? null : model.Comment.Trim();
        application.Reviews.Add(new ApplicationReview { ReviewerId = managerId, Outcome = outcome, Comment = comment, CreatedAtUtc = Now });
        var status = outcome switch
        {
            ReviewOutcome.Approved => RentalApplicationStatus.Approved,
            ReviewOutcome.Returned => RentalApplicationStatus.Returned,
            _ => RentalApplicationStatus.Denied
        };
        Transition(application, status, managerId, outcome, comment);
        if (status == RentalApplicationStatus.Returned) application.CurrentStep = ApplicationStep.ApplicantInformation;
        return new(Id: application.Id);
    }

    internal static ApplicantInformationViewModel Information(ApplicantInformation? info) => new()
    {
        Name = info?.Name ?? "",
        Phone = info?.Phone ?? "",
        Email = info?.Email ?? "",
        CurrentAddress = info?.CurrentAddress ?? ""
    };

    private static ApplicationMutationResult? Validate(object model, string prefix = "")
    {
        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(model, new ValidationContext(model), results, true)) return null;
        var errors = results.SelectMany(r => r.MemberNames.DefaultIfEmpty("").Select(key => (Key: prefix + key, Error: r.ErrorMessage!)))
            .GroupBy(e => e.Key).ToDictionary(g => g.Key, g => g.Select(e => e.Error).ToArray());
        return new(400, Errors: errors);
    }

    private static bool ValidResidences(RentalApplication application) => application.Residences.All(residence => Validate(new ResidenceFormViewModel
    {
        Address = residence.Address,
        LandlordName = residence.LandlordName,
        LandlordPhone = residence.LandlordPhone,
        MoveInDate = residence.MoveInDate,
        MoveOutDate = residence.MoveOutDate
    }) is null);

    private void Transition(
        RentalApplication application,
        RentalApplicationStatus status,
        string actor,
        ReviewOutcome? outcome = null,
        string? comment = null)
    {
        application.StatusHistory.Add(new ApplicationStatusHistory
        {
            PreviousStatus = application.Status,
            NewStatus = status,
            ChangedByUserId = actor,
            ReviewOutcome = outcome,
            Comment = comment,
            CreatedAtUtc = Now
        });
        application.Status = status;
        application.UpdatedAtUtc = Now;
    }
}

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

    public ApplicationMutationResult Wizard(RentalApplication a, string userId, string command,
        ApplicantInformationViewModel information, bool unitAvailable)
    {
        if (a.ApplicantId != userId) return new(404);
        if (!IsEditable(a.Status)) return new(409, Message: "This application is read-only. Reload to see its current status.");
        if (command == "back")
        {
            a.CurrentStep = a.CurrentStep == ApplicationStep.Summary ? ApplicationStep.ResidenceHistory : ApplicationStep.ApplicantInformation;
        }
        else if (command == "continue" && a.CurrentStep == ApplicationStep.ApplicantInformation)
        {
            var invalid = Validate(information, "ApplicantInformation.");
            if (invalid is not null) return invalid;
            a.ApplicantInformation ??= new ApplicantInformation { Name = "", Phone = "", Email = "", CurrentAddress = "" };
            a.ApplicantInformation.Name = information.Name.Trim();
            a.ApplicantInformation.Phone = information.Phone.Trim();
            a.ApplicantInformation.Email = information.Email.Trim();
            a.ApplicantInformation.CurrentAddress = information.CurrentAddress.Trim();
            a.ApplicantInformationSaved = true;
            a.CurrentStep = ApplicationStep.ResidenceHistory;
        }
        else if (command == "continue" && a.CurrentStep == ApplicationStep.ResidenceHistory)
        {
            if (!ValidResidences(a)) return new(409, Message: "Please correct the residence history before continuing.");
            a.ResidenceHistorySaved = true;
            a.CurrentStep = ApplicationStep.Summary;
        }
        else if (command == "submit")
        {
            if (a.CurrentStep != ApplicationStep.Summary || !a.ApplicantInformationSaved || !a.ResidenceHistorySaved)
                return new(409, Message: "Complete both sections and review the summary before submitting.");
            if (Validate(Information(a.ApplicantInformation)) is not null || !ValidResidences(a))
                return new(409, Message: "Please go back and correct the application information.");
            if (!unitAvailable) return new(409, Message: "This unit is no longer available. Your application has not been submitted.");
            Transition(a, RentalApplicationStatus.Submitted, userId);
            a.SubmittedAtUtc = Now;
        }
        else return new(400, Message: "This command is not valid for the current step.");
        a.UpdatedAtUtc = Now;
        return new(Id: a.Id);
    }

    public ApplicationMutationResult Withdraw(RentalApplication a, string userId)
    {
        if (a.ApplicantId != userId) return new(404);
        if (!IsEditable(a.Status) && a.Status != RentalApplicationStatus.Submitted)
            return new(409, Message: "This application cannot be withdrawn.");
        Transition(a, RentalApplicationStatus.Withdrawn, userId);
        return new(Id: a.Id);
    }

    public ApplicationMutationResult SaveResidence(RentalApplication a, int? residenceId, string userId,
        ResidenceFormViewModel model, bool delete)
    {
        if (a.ApplicantId != userId) return new(404);
        if (!IsEditable(a.Status)) return new(409, Message: "This application is no longer editable. Reload the page.");
        var residence = residenceId.HasValue ? a.Residences.SingleOrDefault(r => r.Id == residenceId) : null;
        if (residenceId.HasValue && residence is null) return new(404);
        if (delete)
        {
            if (residence is null) return new(404);
            a.Residences.Remove(residence);
        }
        else
        {
            var invalid = Validate(model);
            if (invalid is not null) return invalid;
            if (residence is null)
            {
                residence = new Residence { RentalApplicationId = a.Id, Address = "", LandlordName = "", LandlordPhone = "" };
                a.Residences.Add(residence);
            }
            residence.Address = model.Address.Trim();
            residence.LandlordName = model.LandlordName.Trim();
            residence.LandlordPhone = model.LandlordPhone.Trim();
            residence.MoveInDate = model.MoveInDate!.Value;
            residence.MoveOutDate = model.MoveOutDate!.Value;
        }
        a.ResidenceHistorySaved = false;
        a.CurrentStep = ApplicationStep.ResidenceHistory;
        a.UpdatedAtUtc = Now;
        return new(Id: a.Id);
    }

    public ApplicationMutationResult Review(RentalApplication a, string managerId, ApplicationReviewViewModel model, bool unitAvailable)
    {
        if (a.Status != RentalApplicationStatus.Submitted) return new(409, Message: "Only submitted applications can be reviewed. Reload the page.");
        var invalid = Validate(model);
        if (invalid is not null) return invalid;
        var outcome = model.Outcome!.Value;
        if (outcome == ReviewOutcome.Approved)
        {
            if (!unitAvailable || a.Lease is not null) return new(409, Message: "The unit already has an active lease. Approval was not saved.");
            var today = DateOnly.FromDateTime(Now);
            a.Lease = new Lease { ApplicationId = a.Id, UnitId = a.UnitId, StartDate = today, EndDate = today.AddMonths(12).AddDays(-1), CreatedAtUtc = Now };
        }
        var comment = string.IsNullOrWhiteSpace(model.Comment) ? null : model.Comment.Trim();
        a.Reviews.Add(new ApplicationReview { ReviewerId = managerId, Outcome = outcome, Comment = comment, CreatedAtUtc = Now });
        var status = outcome switch
        {
            ReviewOutcome.Approved => RentalApplicationStatus.Approved,
            ReviewOutcome.Returned => RentalApplicationStatus.Returned,
            _ => RentalApplicationStatus.Denied
        };
        Transition(a, status, managerId, outcome, comment);
        if (status == RentalApplicationStatus.Returned) a.CurrentStep = ApplicationStep.ApplicantInformation;
        return new(Id: a.Id);
    }

    internal static ApplicantInformationViewModel Information(ApplicantInformation? info) => new()
    {
        Name = info?.Name ?? "", Phone = info?.Phone ?? "", Email = info?.Email ?? "", CurrentAddress = info?.CurrentAddress ?? ""
    };

    private static ApplicationMutationResult? Validate(object model, string prefix = "")
    {
        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(model, new ValidationContext(model), results, true)) return null;
        var errors = results.SelectMany(r => r.MemberNames.DefaultIfEmpty("").Select(key => (Key: prefix + key, Error: r.ErrorMessage!)))
            .GroupBy(e => e.Key).ToDictionary(g => g.Key, g => g.Select(e => e.Error).ToArray());
        return new(400, Errors: errors);
    }

    private static bool ValidResidences(RentalApplication a) => a.Residences.All(r => Validate(new ResidenceFormViewModel
    {
        Address = r.Address, LandlordName = r.LandlordName, LandlordPhone = r.LandlordPhone,
        MoveInDate = r.MoveInDate, MoveOutDate = r.MoveOutDate
    }) is null);

    private void Transition(RentalApplication a, RentalApplicationStatus status, string actor, ReviewOutcome? outcome = null, string? comment = null)
    {
        a.StatusHistory.Add(new ApplicationStatusHistory
        {
            PreviousStatus = a.Status, NewStatus = status, ChangedByUserId = actor,
            ReviewOutcome = outcome, Comment = comment, CreatedAtUtc = Now
        });
        a.Status = status;
        a.UpdatedAtUtc = Now;
    }
}

using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PropertyRental.Web.Models;
using PropertyRental.Web.Models.Enums;
using PropertyRental.Web.Services.Interfaces;
using PropertyRental.Web.ViewModels.Applications;
using PropertyRental.Web.ViewModels.Reviews;

namespace PropertyRental.Web.Services;

public partial class ApplicationService
{
    private readonly ApplicationWorkflow workflow = new(timeProvider);
    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;
    private DateOnly Today => DateOnly.FromDateTime(Now);

    private IQueryable<RentalApplication> Owned(int id, string userId) => dbContext.RentalApplications
        .Where(a => a.Id == id && a.ApplicantId == userId);

    public async Task<ApplicationWizardViewModel?> GetWizardAsync(int id, string userId, CancellationToken ct = default)
    {
        var a = await Owned(id, userId).AsNoTracking().Include(a => a.Unit).ThenInclude(u => u.Property)
            .Include(a => a.ApplicantInformation).Include(a => a.Residences).SingleOrDefaultAsync(ct);
        if (a is null) return null;
        return new ApplicationWizardViewModel
        {
            Id = a.Id, Status = a.Status, CurrentStep = a.CurrentStep,
            UnitSummary = $"{a.Unit.Property.Name} / Unit {a.Unit.UnitNumber}",
            ApplicantInformationSaved = a.ApplicantInformationSaved, ResidenceHistorySaved = a.ResidenceHistorySaved,
            ApplicantInformation = ApplicationWorkflow.Information(a.ApplicantInformation),
            ResidenceHistory = new ResidenceHistoryViewModel
            {
                ApplicationId = a.Id, IsEditable = ApplicationWorkflow.IsEditable(a.Status),
                Items = a.Residences.OrderByDescending(r => r.MoveOutDate).ThenBy(r => r.Id).Select(r => new ResidenceFormViewModel
                {
                    ApplicationId = a.Id, ResidenceId = r.Id, Address = r.Address,
                    LandlordName = r.LandlordName, LandlordPhone = r.LandlordPhone,
                    MoveInDate = r.MoveInDate, MoveOutDate = r.MoveOutDate
                }).ToList()
            }
        };
    }

    // Serializable protects the lease range, including when no lease exists yet.
    // Availability and all workflow mutations commit together or roll back together.
    private async Task<ApplicationMutationResult> TransactAsync(Func<Task<ApplicationMutationResult>> operation, CancellationToken ct)
    {
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var result = await operation();
            if (!result.Succeeded)
            {
                dbContext.ChangeTracker.Clear();
                return result;
            }
            await dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            return new(409, Message: "The application changed or could not be saved. Reload and try again.");
        }
        catch (Exception ex) when (IsSqlConflict(ex))
        {
            dbContext.ChangeTracker.Clear();
            return new(409, Message: "Another operation is in progress. Reload and try again.");
        }
    }

    private static bool IsSqlConflict(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
            if (current is SqlException sql && sql.Number is 1205 or 1222 or 2601 or 2627) return true;
        return false;
    }

    public Task<ApplicationMutationResult> CreateDraftAsync(int unitId, string userId, CancellationToken ct = default) =>
        TransactAsync(() => CreateDraftCoreAsync(unitId, userId, ct), ct);

    private async Task<ApplicationMutationResult> CreateDraftCoreAsync(int unitId, string userId, CancellationToken ct)
    {
        if (!await unitService.IsAvailableAsync(unitId, Today, ct)) return new(409, Message: "This unit is no longer available.");
        var application = new RentalApplication
        {
            ApplicantId = userId, UnitId = unitId, Status = RentalApplicationStatus.Draft,
            CurrentStep = ApplicationStep.ApplicantInformation, CreatedAtUtc = Now, UpdatedAtUtc = Now
        };
        application.StatusHistory.Add(new ApplicationStatusHistory { NewStatus = application.Status, ChangedByUserId = userId, CreatedAtUtc = Now });
        dbContext.RentalApplications.Add(application);
        await dbContext.SaveChangesAsync(ct);
        return new(Id: application.Id);
    }

    public Task<ApplicationMutationResult> WizardAsync(int id, string userId, WizardCommand command, ApplicantInformationViewModel information, CancellationToken ct = default) =>
        TransactAsync(() => ApplyWizardCommandAsync(id, userId, command, information, ct), ct);

    private async Task<ApplicationMutationResult> ApplyWizardCommandAsync(int id, string userId, WizardCommand command, ApplicantInformationViewModel information, CancellationToken ct)
    {
        var application = await Owned(id, userId)
            .Include(application => application.ApplicantInformation)
            .Include(application => application.Residences)
            .SingleOrDefaultAsync(ct);
        if (application is null) return new(404);
        // Availability is rechecked only for Submit; Back and Continue do not need it.
        var unitAvailable = command != WizardCommand.Submit
            || await unitService.IsAvailableAsync(application.UnitId, Today, ct);
        return workflow.Wizard(application, userId, command, information, unitAvailable);
    }

    public Task<ApplicationMutationResult> WithdrawAsync(int id, string userId, CancellationToken ct = default) =>
        TransactAsync(() => WithdrawCoreAsync(id, userId, ct), ct);

    private async Task<ApplicationMutationResult> WithdrawCoreAsync(int id, string userId, CancellationToken ct)
    {
        var application = await Owned(id, userId).SingleOrDefaultAsync(ct);
        return application is null ? new(404) : workflow.Withdraw(application, userId);
    }

    public Task<ApplicationMutationResult> SaveResidenceAsync(int applicationId, int? residenceId, string userId, ResidenceFormViewModel model, bool delete = false, CancellationToken ct = default) =>
        TransactAsync(() => SaveResidenceCoreAsync(applicationId, residenceId, userId, model, delete, ct), ct);

    private async Task<ApplicationMutationResult> SaveResidenceCoreAsync(int applicationId, int? residenceId, string userId, ResidenceFormViewModel model, bool delete, CancellationToken ct)
    {
        var application = await Owned(applicationId, userId)
            .Include(application => application.Residences)
            .SingleOrDefaultAsync(ct);
        return application is null ? new(404) : workflow.SaveResidence(application, residenceId, userId, model, delete);
    }

    public Task<ApplicationMutationResult> ReviewAsync(int id, string managerId, ApplicationReviewViewModel model, CancellationToken ct = default) =>
        TransactAsync(() => ReviewCoreAsync(id, managerId, model, ct), ct);

    private async Task<ApplicationMutationResult> ReviewCoreAsync(int id, string managerId, ApplicationReviewViewModel model, CancellationToken ct)
    {
        var application = await dbContext.RentalApplications
            .Include(application => application.Lease)
            .SingleOrDefaultAsync(application => application.Id == id, ct);
        if (application is null) return new(404);
        // Availability is rechecked only for Approve; Return and Deny do not need it.
        var unitAvailable = model.Outcome != ReviewOutcome.Approved
            || await unitService.IsAvailableAsync(application.UnitId, Today, ct);
        return workflow.Review(application, managerId, model, unitAvailable);
    }
}

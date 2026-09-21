using PropertyRental.Web.Models;
using PropertyRental.Web.Models.Enums;
using PropertyRental.Web.Services;
using PropertyRental.Web.ViewModels.Applications;
using PropertyRental.Web.ViewModels.Reviews;

namespace PropertyRental.Tests.Services;

public class ApplicationWorkflowTests
{
    private const string Applicant = "applicant-1";
    private const string Manager = "manager-1";
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
    private readonly ApplicationWorkflow workflow = new(new FixedClock(Now));

    [Theory]
    [InlineData(RentalApplicationStatus.Draft)]
    [InlineData(RentalApplicationStatus.Returned)]
    public void Submit_saved_summary_records_transition_and_timestamp(RentalApplicationStatus status)
    {
        var a = Ready(status);
        var result = workflow.Wizard(a, Applicant, WizardCommand.Submit, new(), true);
        Assert.True(result.Succeeded);
        Assert.Equal(RentalApplicationStatus.Submitted, a.Status);
        Assert.Equal(Now.UtcDateTime, a.SubmittedAtUtc);
        var history = Assert.Single(a.StatusHistory);
        Assert.Equal(status, history.PreviousStatus);
        Assert.Equal(RentalApplicationStatus.Submitted, history.NewStatus);
        Assert.Equal(Applicant, history.ChangedByUserId);
        Assert.Equal(Now.UtcDateTime, history.CreatedAtUtc);
        Assert.Empty(a.Reviews);
        Assert.Null(a.Lease);
    }

    [Fact]
    public void Submit_with_active_lease_does_not_change_application()
    {
        var a = Ready();
        var result = workflow.Wizard(a, Applicant, WizardCommand.Submit, new(), false);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(RentalApplicationStatus.Draft, a.Status);
        Assert.Null(a.SubmittedAtUtc);
        Assert.Empty(a.StatusHistory);
        Assert.Equal(default, a.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(ApplicationStep.ApplicantInformation, true, true)]
    [InlineData(ApplicationStep.ResidenceHistory, true, true)]
    [InlineData(ApplicationStep.Summary, false, true)]
    [InlineData(ApplicationStep.Summary, true, false)]
    [InlineData(ApplicationStep.Summary, false, false)]
    public void Submit_requires_summary_and_both_saved_sections(ApplicationStep step, bool info, bool residences)
    {
        var a = Ready();
        a.CurrentStep = step;
        a.ApplicantInformationSaved = info;
        a.ResidenceHistorySaved = residences;
        Assert.Equal(409, workflow.Wizard(a, Applicant, WizardCommand.Submit, new(), true).StatusCode);
        Assert.Equal(RentalApplicationStatus.Draft, a.Status);
        Assert.Empty(a.StatusHistory);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Submit_revalidates_persisted_data_despite_saved_flags(bool invalidInformation)
    {
        var a = Ready();
        if (invalidInformation) a.ApplicantInformation!.Email = "not-an-email";
        else a.Residences.Add(new Residence { Address = "Old address", LandlordName = "Landlord", LandlordPhone = "1234567890", MoveInDate = new(2025, 1, 1), MoveOutDate = new(2024, 1, 1) });
        Assert.Equal(409, workflow.Wizard(a, Applicant, WizardCommand.Submit, new(), true).StatusCode);
        Assert.Empty(a.StatusHistory);
    }

    [Theory]
    [InlineData(RentalApplicationStatus.Draft)]
    [InlineData(RentalApplicationStatus.Returned)]
    public void Editable_application_saves_only_current_section(RentalApplicationStatus status)
    {
        var a = Ready(status);
        a.CurrentStep = ApplicationStep.ApplicantInformation;
        a.ApplicantInformationSaved = false;
        var input = ValidInformation();
        input.Name = " New name ";
        Assert.True(workflow.Wizard(a, Applicant, WizardCommand.Continue, input, false).Succeeded);
        Assert.Equal("New name", a.ApplicantInformation!.Name);
        Assert.True(a.ApplicantInformationSaved);
        Assert.Equal(ApplicationStep.ResidenceHistory, a.CurrentStep);
        // Information posted while continuing Residence History must be ignored.
        Assert.True(workflow.Wizard(a, Applicant, WizardCommand.Continue, new(), false).Succeeded);
        Assert.Equal("New name", a.ApplicantInformation.Name);
        Assert.Equal(ApplicationStep.Summary, a.CurrentStep);
        Assert.True(a.ResidenceHistorySaved);
    }

    [Fact]
    public void Invalid_information_has_field_errors_and_is_not_saved()
    {
        var a = Ready();
        a.CurrentStep = ApplicationStep.ApplicantInformation;
        var result = workflow.Wizard(a, Applicant, WizardCommand.Continue, new(), true);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("ApplicantInformation.Name", result.Errors!.Keys);
        Assert.Equal("Original name", a.ApplicantInformation!.Name);
        Assert.Equal(ApplicationStep.ApplicantInformation, a.CurrentStep);
        Assert.Equal(default, a.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(ApplicationStep.Summary, ApplicationStep.ResidenceHistory)]
    [InlineData(ApplicationStep.ResidenceHistory, ApplicationStep.ApplicantInformation)]
    public void Back_ignores_invalid_posted_data(ApplicationStep start, ApplicationStep expected)
    {
        var a = Ready();
        a.CurrentStep = start;
        Assert.True(workflow.Wizard(a, Applicant, WizardCommand.Back, new(), true).Succeeded);
        Assert.Equal(expected, a.CurrentStep);
        Assert.Equal("Original name", a.ApplicantInformation!.Name);
    }

    [Fact]
    public void Continue_on_summary_step_is_rejected()
    {
        // Unknown command strings never reach the workflow: the controller
        // rejects them with 400 before calling the service.
        var a = Ready();
        Assert.Equal(ApplicationStep.Summary, a.CurrentStep);
        Assert.Equal(400, workflow.Wizard(a, Applicant, WizardCommand.Continue, ValidInformation(), true).StatusCode);
        Assert.Equal(ApplicationStep.Summary, a.CurrentStep);
        Assert.Empty(a.StatusHistory);
    }

    [Theory]
    [InlineData(RentalApplicationStatus.Submitted)]
    [InlineData(RentalApplicationStatus.Approved)]
    [InlineData(RentalApplicationStatus.Denied)]
    [InlineData(RentalApplicationStatus.Withdrawn)]
    public void Read_only_states_reject_all_applicant_mutations(RentalApplicationStatus status)
    {
        var a = Ready(status);
        foreach (var command in new[] { WizardCommand.Back, WizardCommand.Continue, WizardCommand.Submit })
            Assert.Equal(409, workflow.Wizard(a, Applicant, command, ValidInformation(), true).StatusCode);
        Assert.Equal(409, workflow.SaveResidence(a, null, Applicant, ValidResidence(), false).StatusCode);
        Assert.Equal(409, workflow.SaveResidence(a, 1, Applicant, new(), true).StatusCode);
        Assert.Equal(status, a.Status);
        Assert.Empty(a.StatusHistory);
        Assert.Empty(a.Residences);
    }

    [Fact]
    public void Foreign_applicant_cannot_mutate_application()
    {
        var a = Ready();
        Assert.Equal(404, workflow.Wizard(a, "other", WizardCommand.Submit, new(), true).StatusCode);
        Assert.Equal(404, workflow.Withdraw(a, "other").StatusCode);
        Assert.Equal(404, workflow.SaveResidence(a, null, "other", ValidResidence(), false).StatusCode);
        Assert.Empty(a.StatusHistory);
        Assert.Empty(a.Residences);
    }

    [Theory]
    [InlineData(2026, 10, 1, 2027, 9, 30)]
    [InlineData(2024, 2, 29, 2025, 2, 27)]
    [InlineData(2026, 12, 31, 2027, 12, 30)]
    public void Approve_creates_twelve_calendar_month_lease_and_matching_audit(int y, int m, int d, int ey, int em, int ed)
    {
        var clock = new FixedClock(new(y, m, d, 23, 59, 0, TimeSpan.Zero));
        var a = Ready(RentalApplicationStatus.Submitted);
        var result = new ApplicationWorkflow(clock).Review(a, Manager, new() { Outcome = ReviewOutcome.Approved }, true);
        Assert.True(result.Succeeded);
        Assert.Equal(RentalApplicationStatus.Approved, a.Status);
        Assert.NotNull(a.Lease);
        Assert.Equal(new DateOnly(y, m, d), a.Lease.StartDate);
        Assert.Equal(new DateOnly(ey, em, ed), a.Lease.EndDate);
        Assert.Equal(a.Id, a.Lease.ApplicationId);
        Assert.Equal(a.UnitId, a.Lease.UnitId);
        Assert.Equal(clock.GetUtcNow().UtcDateTime, a.Lease.CreatedAtUtc);
        var review = Assert.Single(a.Reviews);
        Assert.Equal(ReviewOutcome.Approved, review.Outcome);
        Assert.Null(review.Comment);
        Assert.Equal(Manager, review.ReviewerId);
        var history = Assert.Single(a.StatusHistory);
        Assert.Equal(RentalApplicationStatus.Submitted, history.PreviousStatus);
        Assert.Equal(RentalApplicationStatus.Approved, history.NewStatus);
        Assert.Equal(review.Outcome, history.ReviewOutcome);
        Assert.Equal(review.ReviewerId, history.ChangedByUserId);
    }

    [Fact]
    public void Approval_with_active_lease_has_no_partial_side_effects()
    {
        var a = Ready(RentalApplicationStatus.Submitted);
        Assert.Equal(409, workflow.Review(a, Manager, new() { Outcome = ReviewOutcome.Approved }, false).StatusCode);
        Assert.Equal(RentalApplicationStatus.Submitted, a.Status);
        Assert.Null(a.Lease);
        Assert.Empty(a.Reviews);
        Assert.Empty(a.StatusHistory);
    }

    [Fact]
    public void Repeated_approval_cannot_create_second_lease_or_review()
    {
        var a = Ready(RentalApplicationStatus.Submitted);
        Assert.True(workflow.Review(a, Manager, new() { Outcome = ReviewOutcome.Approved }, true).Succeeded);
        var lease = a.Lease;
        Assert.Equal(409, workflow.Review(a, Manager, new() { Outcome = ReviewOutcome.Approved }, true).StatusCode);
        Assert.Same(lease, a.Lease);
        Assert.Single(a.Reviews);
        Assert.Single(a.StatusHistory);
    }

    [Theory]
    [InlineData(ReviewOutcome.Returned, RentalApplicationStatus.Returned)]
    [InlineData(ReviewOutcome.Denied, RentalApplicationStatus.Denied)]
    public void Return_and_deny_record_comment_without_creating_lease(ReviewOutcome outcome, RentalApplicationStatus status)
    {
        var a = Ready(RentalApplicationStatus.Submitted);
        Assert.True(workflow.Review(a, Manager, new() { Outcome = outcome, Comment = " Reason " }, false).Succeeded);
        Assert.Equal(status, a.Status);
        Assert.Null(a.Lease);
        Assert.Equal("Reason", Assert.Single(a.Reviews).Comment);
        var history = Assert.Single(a.StatusHistory);
        Assert.Equal("Reason", history.Comment);
        Assert.Equal(outcome, history.ReviewOutcome);
        if (outcome == ReviewOutcome.Returned) Assert.Equal(ApplicationStep.ApplicantInformation, a.CurrentStep);
    }

    [Theory]
    [InlineData(ReviewOutcome.Returned, null)]
    [InlineData(ReviewOutcome.Returned, "")]
    [InlineData(ReviewOutcome.Returned, "  \t")]
    [InlineData(ReviewOutcome.Denied, null)]
    [InlineData(ReviewOutcome.Denied, "")]
    [InlineData(ReviewOutcome.Denied, "  \t")]
    public void Return_and_deny_require_nonblank_comment(ReviewOutcome outcome, string? comment)
    {
        var a = Ready(RentalApplicationStatus.Submitted);
        var result = workflow.Review(a, Manager, new() { Outcome = outcome, Comment = comment }, true);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("Comment", result.Errors!.Keys);
        Assert.Equal(RentalApplicationStatus.Submitted, a.Status);
        Assert.Empty(a.Reviews);
        Assert.Empty(a.StatusHistory);
    }

    [Theory]
    [InlineData(null)]
    [InlineData((ReviewOutcome)999)]
    public void Invalid_review_outcome_cannot_change_status(ReviewOutcome? outcome)
    {
        var a = Ready(RentalApplicationStatus.Submitted);
        Assert.Equal(400, workflow.Review(a, Manager, new() { Outcome = outcome, Comment = "Reason" }, true).StatusCode);
        Assert.Empty(a.Reviews);
        Assert.Equal(RentalApplicationStatus.Submitted, a.Status);
    }

    [Theory]
    [InlineData(RentalApplicationStatus.Draft)]
    [InlineData(RentalApplicationStatus.Returned)]
    [InlineData(RentalApplicationStatus.Approved)]
    [InlineData(RentalApplicationStatus.Denied)]
    [InlineData(RentalApplicationStatus.Withdrawn)]
    public void Review_requires_submitted_status(RentalApplicationStatus status)
    {
        var a = Ready(status);
        foreach (var outcome in Enum.GetValues<ReviewOutcome>())
            Assert.Equal(409, workflow.Review(a, Manager, new() { Outcome = outcome, Comment = "Reason" }, true).StatusCode);
        Assert.Equal(status, a.Status);
        Assert.Empty(a.Reviews);
        Assert.Empty(a.StatusHistory);
    }

    [Fact]
    public void Returned_application_can_be_corrected_and_resubmitted_with_complete_history()
    {
        var a = Ready(RentalApplicationStatus.Submitted);
        Assert.True(workflow.Review(a, Manager, new() { Outcome = ReviewOutcome.Returned, Comment = "Fix address" }, true).Succeeded);
        var input = ValidInformation();
        input.CurrentAddress = "Corrected address";
        Assert.True(workflow.Wizard(a, Applicant, WizardCommand.Continue, input, true).Succeeded);
        Assert.True(workflow.Wizard(a, Applicant, WizardCommand.Continue, new(), true).Succeeded);
        Assert.True(workflow.Wizard(a, Applicant, WizardCommand.Submit, new(), true).Succeeded);
        Assert.Equal("Corrected address", a.ApplicantInformation!.CurrentAddress);
        Assert.Equal(new[] { RentalApplicationStatus.Returned, RentalApplicationStatus.Submitted }, a.StatusHistory.Select(h => h.NewStatus));
        Assert.Single(a.Reviews);
    }

    [Theory]
    [InlineData(RentalApplicationStatus.Draft, true)]
    [InlineData(RentalApplicationStatus.Returned, true)]
    [InlineData(RentalApplicationStatus.Submitted, true)]
    [InlineData(RentalApplicationStatus.Approved, false)]
    [InlineData(RentalApplicationStatus.Denied, false)]
    [InlineData(RentalApplicationStatus.Withdrawn, false)]
    public void Withdraw_obeys_status_transition_rules(RentalApplicationStatus status, bool allowed)
    {
        var a = Ready(status);
        Assert.Equal(allowed, workflow.Withdraw(a, Applicant).Succeeded);
        Assert.Equal(allowed ? RentalApplicationStatus.Withdrawn : status, a.Status);
        if (allowed)
        {
            Assert.Equal(status, Assert.Single(a.StatusHistory).PreviousStatus);
            Assert.Equal(409, workflow.Withdraw(a, Applicant).StatusCode);
            Assert.Single(a.StatusHistory);
        }
        else Assert.Empty(a.StatusHistory);
    }

    [Theory]
    [InlineData(RentalApplicationStatus.Draft)]
    [InlineData(RentalApplicationStatus.Returned)]
    public void Residence_changes_invalidate_saved_section_and_support_add_edit_delete(RentalApplicationStatus status)
    {
        var a = Ready(status);
        Assert.True(workflow.SaveResidence(a, null, Applicant, ValidResidence(), false).Succeeded);
        var residence = Assert.Single(a.Residences);
        residence.Id = 10;
        Assert.False(a.ResidenceHistorySaved);
        Assert.Equal(ApplicationStep.ResidenceHistory, a.CurrentStep);
        a.ResidenceHistorySaved = true;
        var edit = ValidResidence();
        edit.Address = "Updated address";
        Assert.True(workflow.SaveResidence(a, 10, Applicant, edit, false).Succeeded);
        Assert.Equal("Updated address", residence.Address);
        Assert.False(a.ResidenceHistorySaved);
        a.ResidenceHistorySaved = true;
        Assert.True(workflow.SaveResidence(a, 10, Applicant, new(), true).Succeeded);
        Assert.Empty(a.Residences);
        Assert.False(a.ResidenceHistorySaved);
    }

    [Fact]
    public void Invalid_residence_dates_do_not_create_record_or_invalidate_saved_data()
    {
        var a = Ready();
        var input = ValidResidence();
        input.MoveOutDate = input.MoveInDate!.Value.AddDays(-1);
        Assert.Equal(400, workflow.SaveResidence(a, null, Applicant, input, false).StatusCode);
        Assert.Empty(a.Residences);
        Assert.True(a.ResidenceHistorySaved);
        Assert.Equal(ApplicationStep.Summary, a.CurrentStep);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Residence_id_from_another_application_is_rejected(bool delete)
    {
        var a = Ready();
        Assert.Equal(404, workflow.SaveResidence(a, 999, Applicant, ValidResidence(), delete).StatusCode);
        Assert.Empty(a.Residences);
        Assert.True(a.ResidenceHistorySaved);
    }

    private static RentalApplication Ready(RentalApplicationStatus status = RentalApplicationStatus.Draft) => new()
    {
        Id = 42, UnitId = 7, ApplicantId = Applicant, Status = status, CurrentStep = ApplicationStep.Summary,
        ApplicantInformationSaved = true, ResidenceHistorySaved = true,
        ApplicantInformation = new() { Name = "Original name", Phone = "1234567890", Email = "applicant@example.com", CurrentAddress = "Current address" }
    };

    private static ApplicantInformationViewModel ValidInformation() => new()
    {
        Name = "Applicant", Phone = "1234567890", Email = "applicant@example.com", CurrentAddress = "Current address"
    };

    private static ResidenceFormViewModel ValidResidence() => new()
    {
        Address = "Old address", LandlordName = "Landlord", LandlordPhone = "1234567890",
        MoveInDate = new(2024, 1, 1), MoveOutDate = new(2025, 1, 1)
    };

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}

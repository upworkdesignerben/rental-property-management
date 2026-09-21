using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyRental.Web.Security;
using PropertyRental.Web.Services.Interfaces;
using PropertyRental.Web.ViewModels.Applications;

namespace PropertyRental.Web.Controllers;

public partial class ApplicationsController
{
    private string ApplicantId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = RoleNames.Applicant)]
    public async Task<IActionResult> Create(int unitId, CancellationToken ct)
    {
        var result = await applicationService.CreateDraftAsync(unitId, ApplicantId, ct);
        if (result.Succeeded) return RedirectToAction(nameof(Wizard), new { id = result.Id });
        TempData["Error"] = result.Message;
        return RedirectToAction("Available", "Units");
    }

    [HttpGet, Authorize(Roles = RoleNames.Applicant)]
    public async Task<IActionResult> Wizard(int id, CancellationToken ct)
    {
        var model = await applicationService.GetWizardAsync(id, ApplicantId, ct);
        if (model is null) return NotFound();
        return model.IsEditable ? View(model) : RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = RoleNames.Applicant)]
    public async Task<IActionResult> Wizard(int id, string? command,
        [Bind("Name,Phone,Email,CurrentAddress", Prefix = "ApplicantInformation")] ApplicantInformationViewModel information, CancellationToken ct)
    {
        command = command?.Trim().ToLowerInvariant();
        // Only the persisted current section is validated by the service; Back discards posted inputs.
        ModelState.Clear();
        var result = await applicationService.WizardAsync(id, ApplicantId, command ?? "", information, ct);
        if (result.StatusCode == 404) return NotFound();
        if (result.Succeeded)
        {
            if (command == "submit")
            {
                TempData["Success"] = "Your application was submitted for review.";
                return RedirectToAction(nameof(Details), new { id });
            }
            return RedirectToAction(nameof(Wizard), new { id });
        }
        var model = await applicationService.GetWizardAsync(id, ApplicantId, ct);
        if (model is null) return NotFound();
        if (!model.IsEditable)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
        if (command == "continue" && model.CurrentStep == Models.Enums.ApplicationStep.ApplicantInformation)
            model.ApplicantInformation = information;
        AddErrors(result);
        Response.StatusCode = result.StatusCode;
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = RoleNames.Applicant)]
    public async Task<IActionResult> Withdraw(int id, CancellationToken ct)
    {
        var result = await applicationService.WithdrawAsync(id, ApplicantId, ct);
        if (result.StatusCode == 404) return NotFound();
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Application withdrawn." : result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet, Authorize(Roles = RoleNames.Applicant)]
    public async Task<IActionResult> ResidenceList(int applicationId, CancellationToken ct)
    {
        var model = await applicationService.GetWizardAsync(applicationId, ApplicantId, ct);
        if (model is null) return NotFound();
        if (!model.IsEditable) return Conflict(new { message = "This application is no longer editable. Reload the page." });
        return PartialView("_ResidenceList", model.ResidenceHistory);
    }

    [HttpGet, Authorize(Roles = RoleNames.Applicant)]
    public async Task<IActionResult> AddResidence(int applicationId, CancellationToken ct) => await ResidenceForm(applicationId, null, ct);

    [HttpGet, Authorize(Roles = RoleNames.Applicant)]
    public async Task<IActionResult> EditResidence(int applicationId, int residenceId, CancellationToken ct) => await ResidenceForm(applicationId, residenceId, ct);

    private async Task<IActionResult> ResidenceForm(int applicationId, int? residenceId, CancellationToken ct)
    {
        var wizard = await applicationService.GetWizardAsync(applicationId, ApplicantId, ct);
        if (wizard is null) return NotFound();
        if (!wizard.IsEditable) return Conflict(new { message = "This application is no longer editable. Reload the page." });
        var model = residenceId.HasValue ? wizard.ResidenceHistory.Items.SingleOrDefault(r => r.ResidenceId == residenceId)
            : new ResidenceFormViewModel { ApplicationId = applicationId };
        return model is null ? NotFound() : PartialView("_ResidenceForm", model);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = RoleNames.Applicant)]
    public Task<IActionResult> AddResidence(int applicationId, ResidenceFormViewModel model, CancellationToken ct) => SaveResidence(applicationId, null, model, ct);

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = RoleNames.Applicant)]
    public Task<IActionResult> EditResidence(int applicationId, int residenceId, ResidenceFormViewModel model, CancellationToken ct) => SaveResidence(applicationId, residenceId, model, ct);

    private async Task<IActionResult> SaveResidence(int applicationId, int? residenceId, ResidenceFormViewModel model, CancellationToken ct)
    {
        var wizard = await applicationService.GetWizardAsync(applicationId, ApplicantId, ct);
        if (wizard is null || (residenceId.HasValue && !wizard.ResidenceHistory.Items.Any(r => r.ResidenceId == residenceId))) return NotFound();
        if (!wizard.IsEditable) return Conflict(new { message = "This application is no longer editable. Reload the page." });
        model.ApplicationId = applicationId;
        model.ResidenceId = residenceId;
        if (ModelState.IsValid)
        {
            var result = await applicationService.SaveResidenceAsync(applicationId, residenceId, ApplicantId, model, ct: ct);
            if (result.Succeeded) return Json(new { success = true });
            if (result.StatusCode != 400) return StatusCode(result.StatusCode, new { message = result.Message });
            AddErrors(result);
        }
        Response.StatusCode = 400;
        return PartialView("_ResidenceForm", model);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = RoleNames.Applicant)]
    public async Task<IActionResult> DeleteResidence(int applicationId, int residenceId, CancellationToken ct)
    {
        var result = await applicationService.SaveResidenceAsync(applicationId, residenceId, ApplicantId, new(), delete: true, ct: ct);
        return result.Succeeded ? Json(new { success = true }) : StatusCode(result.StatusCode, new { message = result.Message });
    }

    [HttpGet, Authorize(Roles = RoleNames.Applicant)]
    public async Task<IActionResult> ConfirmDeleteResidence(int applicationId, int residenceId, CancellationToken ct)
    {
        var wizard = await applicationService.GetWizardAsync(applicationId, ApplicantId, ct);
        if (wizard is null) return NotFound();
        if (!wizard.IsEditable) return Conflict(new { message = "This application is no longer editable. Reload the page." });
        var model = wizard.ResidenceHistory.Items.SingleOrDefault(r => r.ResidenceId == residenceId);
        return model is null ? NotFound() : PartialView("_DeleteResidence", model);
    }

    private void AddErrors(ApplicationMutationResult result)
    {
        if (result.Message is not null) ModelState.AddModelError("", result.Message);
        if (result.Errors is not null)
            foreach (var (key, errors) in result.Errors)
                foreach (var error in errors) ModelState.AddModelError(key, error);
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyRental.Web.Models.Enums;
using PropertyRental.Web.Security;
using PropertyRental.Web.Services.Interfaces;
using PropertyRental.Web.ViewModels.Reviews;

namespace PropertyRental.Web.Controllers;

[Authorize(Roles = RoleNames.PropertyManager)]
public class ReviewsController(IApplicationService applicationService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Create(int id, CancellationToken ct)
    {
        var model = await applicationService.GetDetailsAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!, true, ct);
        if (model is null) return NotFound();
        if (model.Status != RentalApplicationStatus.Submitted) return Conflict(new { message = "Only submitted applications can be reviewed." });
        return PartialView("~/Views/Applications/_ReviewForm.cshtml", new ApplicationReviewViewModel { ApplicationId = id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int id, ApplicationReviewViewModel model, CancellationToken ct)
    {
        model.ApplicationId = id;
        if (ModelState.IsValid)
        {
            var result = await applicationService.ReviewAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!, model, ct);
            if (result.Succeeded) return Json(new { success = true });
            if (result.StatusCode != 400) return StatusCode(result.StatusCode, new { message = result.Message });
            if (result.Errors is not null)
                foreach (var (key, errors) in result.Errors)
                    foreach (var error in errors) ModelState.AddModelError(key, error);
        }
        Response.StatusCode = 400;
        return PartialView("~/Views/Applications/_ReviewForm.cshtml", model);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyRental.Web.Security;
using PropertyRental.Web.Services.Interfaces;
using PropertyRental.Web.ViewModels.Units;

namespace PropertyRental.Web.Controllers;

public class UnitsController(IUnitService unitService) : Controller
{
    [Authorize(Roles = RoleNames.PropertyManager)]
    public async Task<IActionResult> Index(int? propertyId, CancellationToken cancellationToken)
    {
        return View(await unitService.GetManagerListAsync(propertyId, cancellationToken));
    }

    [Authorize(Roles = RoleNames.PropertyManager)]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return PartialView("_UnitForm", await unitService.GetCreateFormAsync(cancellationToken));
    }

    [Authorize(Roles = RoleNames.PropertyManager)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UnitFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await InvalidFormAsync(model, cancellationToken);
        }

        var result = await unitService.CreateAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return await InvalidFormAsync(model, cancellationToken);
        }

        return Json(new { success = true });
    }

    [Authorize(Roles = RoleNames.PropertyManager)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await unitService.GetEditFormAsync(id, cancellationToken);
        return model is null ? NotFound() : PartialView("_UnitForm", model);
    }

    [Authorize(Roles = RoleNames.PropertyManager)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UnitFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await InvalidFormAsync(model with { Id = id }, cancellationToken);
        }

        var result = await unitService.UpdateAsync(id, model, cancellationToken);
        if (!result.Found)
        {
            return NotFound();
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return await InvalidFormAsync(model with { Id = id }, cancellationToken);
        }

        return Json(new { success = true });
    }

    [Authorize(Roles = RoleNames.PropertyManager)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await unitService.DeleteAsync(id, cancellationToken);
        if (!result.Found)
        {
            return NotFound();
        }

        if (result.HasDependencies)
        {
            return Conflict(new { success = false, message = "A unit with applications or leases cannot be deleted." });
        }

        return Json(new { success = true });
    }

    [Authorize(Roles = RoleNames.Applicant)]
    public async Task<IActionResult> Available(CancellationToken cancellationToken)
    {
        return View(await unitService.GetAvailableListAsync(cancellationToken));
    }

    [Authorize(Roles = RoleNames.Applicant)]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var model = await unitService.GetAvailableDetailsAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    private async Task<IActionResult> InvalidFormAsync(UnitFormViewModel model, CancellationToken cancellationToken)
    {
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return PartialView("_UnitForm", await unitService.PopulateOptionsAsync(model, cancellationToken));
    }
}

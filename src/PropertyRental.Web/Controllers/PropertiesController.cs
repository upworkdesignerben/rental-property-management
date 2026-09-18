using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyRental.Web.Security;
using PropertyRental.Web.Services.Interfaces;
using PropertyRental.Web.ViewModels.Properties;

namespace PropertyRental.Web.Controllers;

[Authorize(Roles = RoleNames.PropertyManager)]
public class PropertiesController(IPropertyService propertyService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await propertyService.GetListAsync(cancellationToken));
    }

    [HttpGet]
    public IActionResult Create()
    {
        return PartialView("_PropertyForm", new PropertyFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PropertyFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return InvalidForm(model);
        }

        await propertyService.CreateAsync(model, cancellationToken);
        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await propertyService.GetForEditAsync(id, cancellationToken);
        return model is null ? NotFound() : PartialView("_PropertyForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PropertyFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return InvalidForm(model with { Id = id });
        }

        return await propertyService.UpdateAsync(id, model, cancellationToken)
            ? Json(new { success = true })
            : NotFound();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await propertyService.DeleteAsync(id, cancellationToken);
        if (!result.Found)
        {
            return NotFound();
        }

        if (result.HasUnits)
        {
            return Conflict(new { success = false, message = "A property with units cannot be deleted." });
        }

        return Json(new { success = true });
    }

    private IActionResult InvalidForm(PropertyFormViewModel model)
    {
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return PartialView("_PropertyForm", model);
    }
}

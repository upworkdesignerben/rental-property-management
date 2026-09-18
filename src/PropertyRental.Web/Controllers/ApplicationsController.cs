using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyRental.Web.Security;
using PropertyRental.Web.Services.Interfaces;
using PropertyRental.Web.ViewModels.Applications;

namespace PropertyRental.Web.Controllers;

[Authorize]
public class ApplicationsController(IApplicationService applicationService) : Controller
{
    public async Task<IActionResult> Index(ApplicationListFilterViewModel filter, CancellationToken cancellationToken)
    {
        if (!TryGetAccess(out var userId, out var isManager))
        {
            return Forbid();
        }

        return View(await applicationService.GetListAsync(userId, isManager, filter, cancellationToken));
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        if (!TryGetAccess(out var userId, out var isManager))
        {
            return Forbid();
        }

        var model = await applicationService.GetDetailsAsync(id, userId, isManager, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    private bool TryGetAccess(out string userId, out bool isManager)
    {
        userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var hasManagerRole = User.IsInRole(RoleNames.PropertyManager);
        var hasApplicantRole = User.IsInRole(RoleNames.Applicant);
        isManager = hasManagerRole;
        return userId.Length != 0 && hasManagerRole != hasApplicantRole;
    }
}

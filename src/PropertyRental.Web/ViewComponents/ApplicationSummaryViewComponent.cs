using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PropertyRental.Web.Security;
using PropertyRental.Web.Services.Interfaces;
using PropertyRental.Web.ViewModels.Applications;

namespace PropertyRental.Web.ViewComponents;

public class ApplicationSummaryViewComponent(IApplicationService applicationService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int applicationId)
    {
        var user = HttpContext.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null || !user.IsInRole(RoleNames.Applicant)) return Content("");
        var model = await applicationService.GetWizardAsync(applicationId, userId, HttpContext.RequestAborted);
        if (model is null) return Content("");
        model.ApplicantInformation.IsReadOnly = true;
        return View(new ApplicationSummaryViewModel(model.ApplicantInformation, new ResidenceHistoryViewModel
        {
            ApplicationId = applicationId, Items = model.ResidenceHistory.Items, IsEditable = false
        }));
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PropertyRental.Web.Models;

namespace PropertyRental.Web.Areas.Identity.Pages.Account;

[Authorize]
public class LogoutModel(SignInManager<ApplicationUser> signInManager) : PageModel
{
    public IActionResult OnGet() => StatusCode(StatusCodes.Status405MethodNotAllowed);

    public async Task<IActionResult> OnPostAsync()
    {
        await signInManager.SignOutAsync();
        return LocalRedirect(Url.Content("~/"));
    }
}

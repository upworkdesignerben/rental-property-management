using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using PropertyRental.Web.Data;
using PropertyRental.Web.Models;
using PropertyRental.Web.Security;

namespace PropertyRental.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterModel(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext dbContext,
    ILogger<RegisterModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; private set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        ReturnUrl = returnUrl;
        returnUrl ??= Url.Content("~/");

        if (!RoleNames.IsRegistrationRole(Input.Role))
        {
            ModelState.AddModelError("Input.Role", "Select a valid account role.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
        };

        var createResult = await userManager.CreateAsync(user, Input.Password);
        if (!createResult.Succeeded)
        {
            AddErrors(createResult);
            return Page();
        }

        var roleResult = await userManager.AddToRoleAsync(user, Input.Role);
        if (!roleResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            AddErrors(roleResult);
            return Page();
        }

        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("A user created a new account with the {Role} role.", Input.Role);
        await signInManager.SignInAsync(user, isPersistent: false);

        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Content("~/"));
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    public sealed class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;
    }
}

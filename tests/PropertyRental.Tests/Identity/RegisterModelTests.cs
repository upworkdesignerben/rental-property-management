using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;


using PropertyRental.Web.Areas.Identity.Pages.Account;
using PropertyRental.Web.Data;
using PropertyRental.Web.Models;
using PropertyRental.Web.Security;

namespace PropertyRental.Tests.Identity;

public class RegisterModelTests
{
    [Fact]
    public async Task OnPostAsyncRejectsUnsupportedRoleBeforeCreatingUser()
    {
        var model = new RegisterModel(
            null!,
            null!,
            null!,
            NullLogger<RegisterModel>.Instance)
        {
            Input = new RegisterModel.InputModel
            {
                Email = "user@test.local",
                Password = "Password1!",
                ConfirmPassword = "Password1!",
                Role = "Administrator",
            },
        };

        var result = await model.OnPostAsync("/");

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.True(model.ModelState.ContainsKey("Input.Role"));
    }

    [Theory]
    [InlineData(RoleNames.Applicant)]
    [InlineData(RoleNames.PropertyManager)]
    public async Task OnPostAsyncCreatesSignedInUserWithSelectedRole(string role)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddCookie(IdentityConstants.ApplicationScheme);
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddSignInManager()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        await using var serviceProvider = services.BuildServiceProvider();
        await IdentityInitializer.InitializeAsync(serviceProvider);

        await using var scope = serviceProvider.CreateAsyncScope();
        var scopedServices = scope.ServiceProvider;
        var httpContext = new DefaultHttpContext { RequestServices = scopedServices };
        scopedServices.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;

        var userManager = scopedServices.GetRequiredService<UserManager<ApplicationUser>>();
        var model = new RegisterModel(
            userManager,
            scopedServices.GetRequiredService<SignInManager<ApplicationUser>>(),
            scopedServices.GetRequiredService<ApplicationDbContext>(),
            NullLogger<RegisterModel>.Instance)
        {
            PageContext = new PageContext { HttpContext = httpContext },
            Input = new RegisterModel.InputModel
            {
                Email = $"{role.ToLowerInvariant()}@test.local",
                Password = "Password1!",
                ConfirmPassword = "Password1!",
                Role = role,
            },
        };

        var result = await model.OnPostAsync("/");

        Assert.IsType<LocalRedirectResult>(result);
        var user = Assert.IsType<ApplicationUser>(await userManager.FindByEmailAsync(model.Input.Email));
        Assert.Equal([role], await userManager.GetRolesAsync(user));
        Assert.Contains("Set-Cookie", httpContext.Response.Headers.Keys);
    }
}

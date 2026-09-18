using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PropertyRental.Web.Data;
using PropertyRental.Web.Models;
using PropertyRental.Web.Security;

namespace PropertyRental.Tests.Data;

public class IdentityInitializerTests
{
    [Fact]
    public async Task InitializeAsyncCreatesEachRoleOnce()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        await using var serviceProvider = services.BuildServiceProvider();

        await IdentityInitializer.InitializeAsync(serviceProvider);
        await IdentityInitializer.InitializeAsync(serviceProvider);

        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleNames = await dbContext.Roles
            .OrderBy(role => role.Name)
            .Select(role => role.Name)
            .ToListAsync();

        Assert.Equal(
            [RoleNames.Applicant, RoleNames.PropertyManager],
            roleNames);
    }
}

using PropertyRental.Web.Security;

namespace PropertyRental.Tests.Security;

public class RoleNamesTests
{
    [Theory]
    [InlineData(RoleNames.Applicant)]
    [InlineData(RoleNames.PropertyManager)]
    public void IsRegistrationRoleAcceptsSupportedRoles(string role)
    {
        Assert.True(RoleNames.IsRegistrationRole(role));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Property Manager")]
    [InlineData("Administrator")]
    public void IsRegistrationRoleRejectsUnsupportedRoles(string? role)
    {
        Assert.False(RoleNames.IsRegistrationRole(role));
    }
}

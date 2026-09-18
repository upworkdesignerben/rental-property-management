namespace PropertyRental.Web.Security;

public static class RoleNames
{
    public const string Applicant = "Applicant";
    public const string PropertyManager = "PropertyManager";

    public static IReadOnlyList<string> All { get; } = [Applicant, PropertyManager];

    public static bool IsRegistrationRole(string? role) =>
        role is Applicant or PropertyManager;
}

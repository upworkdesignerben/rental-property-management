using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertyRental.Web.Models;
using PropertyRental.Web.Models.Enums;
using PropertyRental.Web.Security;

namespace PropertyRental.Web.Data;

public static class DbInitializer
{
    public const string DemoManagerEmail = "manager@demo.local";
    public const string DemoApplicantEmail = "applicant@demo.local";
    public const string DemoPassword = "DemoPassword1!";

    public static async Task InitializeAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await IdentityInitializer.InitializeAsync(services, cancellationToken);

        await using var scope = services.CreateAsyncScope();
        var serviceProvider = scope.ServiceProvider;
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        const string seedName = "DemoData-v1";
        if (await dbContext.SeedHistory.AnyAsync(seed => seed.Name == seedName, cancellationToken)) return;
        // Adopt databases seeded before seed checkpoints were introduced without restoring
        // deleted residences or creating new applications for statuses users have changed.
        if (await dbContext.RentalApplications.AnyAsync(cancellationToken))
        {
            dbContext.SeedHistory.Add(new SeedHistory { Name = seedName, AppliedAtUtc = DateTime.UtcNow });
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var manager = await EnsureUserAsync(userManager, DemoManagerEmail, RoleNames.PropertyManager);
        var applicant = await EnsureUserAsync(userManager, DemoApplicantEmail, RoleNames.Applicant);

        var now = serviceProvider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);
        Randomizer.Seed = new Random(2_026_091_8);
        var faker = new Faker("en");

        var unitTypeDefinitions = new[]
        {
            (Name: "Standard", IsActive: true),
            (Name: "Premium", IsActive: true),
            (Name: "Deluxe", IsActive: true),
            (Name: "Legacy", IsActive: false)
        };
        var existingUnitTypeNames = await dbContext.UnitTypes
            .Select(unitType => unitType.Name)
            .ToListAsync(cancellationToken);
        dbContext.UnitTypes.AddRange(unitTypeDefinitions
            .Where(definition => !existingUnitTypeNames.Contains(definition.Name))
            .Select(definition => new UnitType { Name = definition.Name, IsActive = definition.IsActive }));

        var propertyDefinitions = new[]
        {
            (Name: "Riverside Apartments", Address: "101 Riverside Drive, Springfield"),
            (Name: "Maple Court", Address: "22 Maple Avenue, Springfield")
        };
        var existingPropertyNames = await dbContext.Properties
            .Select(property => property.Name)
            .ToListAsync(cancellationToken);
        dbContext.Properties.AddRange(propertyDefinitions
            .Where(definition => !existingPropertyNames.Contains(definition.Name))
            .Select(definition => new Property
            {
                Name = definition.Name,
                Address = definition.Address,
                Description = faker.Lorem.Sentence(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }));
        await dbContext.SaveChangesAsync(cancellationToken);

        var unitTypes = await dbContext.UnitTypes.ToDictionaryAsync(unitType => unitType.Name, cancellationToken);
        var properties = await dbContext.Properties.ToDictionaryAsync(property => property.Name, cancellationToken);
        var unitDefinitions = new[]
        {
            (Property: "Riverside Apartments", UnitType: "Premium", Number: "101", Bedrooms: 2, Rent: 1_450m),
            (Property: "Riverside Apartments", UnitType: "Standard", Number: "102", Bedrooms: 1, Rent: 1_100m),
            (Property: "Maple Court", UnitType: "Deluxe", Number: "201", Bedrooms: 3, Rent: 2_100m),
            (Property: "Maple Court", UnitType: "Standard", Number: "202", Bedrooms: 0, Rent: 900m)
        };
        var existingUnits = await dbContext.Units
            .Select(unit => new { unit.PropertyId, unit.UnitNumber })
            .ToListAsync(cancellationToken);
        dbContext.Units.AddRange(unitDefinitions
            .Where(definition => !existingUnits.Any(unit =>
                unit.PropertyId == properties[definition.Property].Id && unit.UnitNumber == definition.Number))
            .Select(definition => CreateUnit(
                properties[definition.Property],
                unitTypes[definition.UnitType],
                definition.Number,
                definition.Bedrooms,
                definition.Rent,
                now)));
        await dbContext.SaveChangesAsync(cancellationToken);

        var units = await dbContext.Units
            .Include(unit => unit.Property)
            .Where(unit => unit.Property.Name == "Riverside Apartments")
            .ToDictionaryAsync(unit => unit.UnitNumber, cancellationToken);
        var applications = new List<RentalApplication>();
        foreach (var status in Enum.GetValues<RentalApplicationStatus>())
        {
            var unitId = status == RentalApplicationStatus.Approved ? units["101"].Id : units["102"].Id;
            var application = await dbContext.RentalApplications.FirstOrDefaultAsync(
                item => item.ApplicantId == applicant.Id && item.Status == status && item.UnitId == unitId,
                cancellationToken);
            if (application is null)
            {
                application = new RentalApplication
                {
                    UnitId = unitId,
                    ApplicantId = applicant.Id,
                    Status = status,
                    CurrentStep = status is RentalApplicationStatus.Draft or RentalApplicationStatus.Returned
                        ? ApplicationStep.ApplicantInformation
                        : ApplicationStep.Summary,
                    ApplicantInformationSaved = true,
                    ResidenceHistorySaved = true,
                    CreatedAtUtc = now.AddDays(-(int)status - 10),
                    UpdatedAtUtc = now.AddDays(-(int)status),
                    SubmittedAtUtc = status == RentalApplicationStatus.Draft ? null : now.AddDays(-7)
                };
                dbContext.RentalApplications.Add(application);
            }

            applications.Add(application);
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var application in applications)
        {
            if (!await dbContext.ApplicantInformations.AnyAsync(
                    information => information.RentalApplicationId == application.Id,
                    cancellationToken))
            {
                dbContext.ApplicantInformations.Add(new ApplicantInformation
                {
                    RentalApplicationId = application.Id,
                    Name = faker.Name.FullName(),
                    Phone = faker.Phone.PhoneNumber("###-###-####"),
                    Email = DemoApplicantEmail,
                    CurrentAddress = faker.Address.FullAddress()
                });
            }

            if (!await dbContext.Residences.AnyAsync(
                    residence => residence.RentalApplicationId == application.Id,
                    cancellationToken))
            {
                dbContext.Residences.Add(new Residence
                {
                    RentalApplicationId = application.Id,
                    Address = faker.Address.FullAddress(),
                    LandlordName = faker.Name.FullName(),
                    LandlordPhone = faker.Phone.PhoneNumber("###-###-####"),
                    MoveInDate = today.AddYears(-2),
                    MoveOutDate = today.AddMonths(-1)
                });
            }

            if (!await dbContext.ApplicationStatusHistories.AnyAsync(
                    history => history.RentalApplicationId == application.Id,
                    cancellationToken))
            {
                AddStatusHistory(dbContext, application, applicant.Id, manager.Id, now);
            }
        }

        var approvedApplication = applications.Single(application => application.Status == RentalApplicationStatus.Approved);
        if (!await dbContext.Leases.AnyAsync(lease => lease.ApplicationId == approvedApplication.Id, cancellationToken))
        {
            dbContext.Leases.Add(new Lease
            {
                ApplicationId = approvedApplication.Id,
                UnitId = approvedApplication.UnitId,
                StartDate = today,
                EndDate = today.AddMonths(12).AddDays(-1),
                CreatedAtUtc = now
            });
        }
        dbContext.SeedHistory.Add(new SeedHistory { Name = seedName, AppliedAtUtc = now });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static Unit CreateUnit(
        Property property,
        UnitType unitType,
        string unitNumber,
        int bedrooms,
        decimal monthlyRent,
        DateTime now) => new()
        {
            PropertyId = property.Id,
            UnitTypeId = unitType.Id,
            UnitNumber = unitNumber,
            Bedrooms = bedrooms,
            MonthlyRent = monthlyRent,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            var createResult = await userManager.CreateAsync(user, DemoPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Could not create demo user '{email}': {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult = await userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join("; ", roleResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Could not assign role '{role}' to '{email}': {errors}");
            }
        }

        return user;
    }

    private static void AddStatusHistory(
        ApplicationDbContext dbContext,
        RentalApplication application,
        string applicantId,
        string managerId,
        DateTime now)
    {
        dbContext.ApplicationStatusHistories.Add(new ApplicationStatusHistory
        {
            RentalApplicationId = application.Id,
            NewStatus = RentalApplicationStatus.Draft,
            ChangedByUserId = applicantId,
            CreatedAtUtc = application.CreatedAtUtc
        });

        if (application.Status == RentalApplicationStatus.Draft)
        {
            return;
        }

        if (application.Status != RentalApplicationStatus.Submitted)
        {
            dbContext.ApplicationStatusHistories.Add(new ApplicationStatusHistory
            {
                RentalApplicationId = application.Id,
                PreviousStatus = RentalApplicationStatus.Draft,
                NewStatus = RentalApplicationStatus.Submitted,
                ChangedByUserId = applicantId,
                CreatedAtUtc = application.SubmittedAtUtc!.Value
            });
        }

        var changedByUserId = application.Status switch
        {
            RentalApplicationStatus.Returned or RentalApplicationStatus.Approved or RentalApplicationStatus.Denied => managerId,
            _ => applicantId
        };
        ReviewOutcome? reviewOutcome = application.Status switch
        {
            RentalApplicationStatus.Returned => ReviewOutcome.Returned,
            RentalApplicationStatus.Approved => ReviewOutcome.Approved,
            RentalApplicationStatus.Denied => ReviewOutcome.Denied,
            _ => null
        };
        var comment = reviewOutcome switch
        {
            ReviewOutcome.Returned => "Please provide one additional document.",
            ReviewOutcome.Denied => "The application does not meet the current criteria.",
            _ => null
        };

        dbContext.ApplicationStatusHistories.Add(new ApplicationStatusHistory
        {
            RentalApplicationId = application.Id,
            PreviousStatus = application.Status == RentalApplicationStatus.Submitted
                ? RentalApplicationStatus.Draft
                : RentalApplicationStatus.Submitted,
            NewStatus = application.Status,
            ChangedByUserId = changedByUserId,
            ReviewOutcome = reviewOutcome,
            Comment = comment,
            CreatedAtUtc = application.UpdatedAtUtc
        });

        if (reviewOutcome is { } outcome)
        {
            dbContext.ApplicationReviews.Add(new ApplicationReview
            {
                RentalApplicationId = application.Id,
                ReviewerId = managerId,
                Outcome = outcome,
                Comment = comment,
                CreatedAtUtc = application.UpdatedAtUtc
            });
        }
    }
}

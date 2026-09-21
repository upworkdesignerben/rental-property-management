using Microsoft.EntityFrameworkCore;
using PropertyRental.Web.Data;
using PropertyRental.Web.Models.Enums;
using PropertyRental.Web.Services.Interfaces;
using PropertyRental.Web.ViewModels.Applications;

namespace PropertyRental.Web.Services;

public partial class ApplicationService(ApplicationDbContext dbContext, TimeProvider timeProvider, IUnitService unitService) : IApplicationService
{
    public async Task<ApplicationListViewModel> GetListAsync(
        string userId,
        bool isManager,
        ApplicationListFilterViewModel filter,
        CancellationToken cancellationToken = default)
    {
        var applications = dbContext.RentalApplications.AsNoTracking().AsQueryable();
        if (!isManager)
        {
            applications = applications.Where(application => application.ApplicantId == userId);
        }

        var propertyOptions = isManager
            ? dbContext.Properties.AsNoTracking()
            : applications.Select(application => application.Unit.Property).Distinct();

        if (filter.Status.HasValue)
        {
            applications = applications.Where(application => application.Status == filter.Status.Value);
        }

        if (filter.PropertyId.HasValue)
        {
            applications = applications.Where(application => application.Unit.PropertyId == filter.PropertyId.Value);
        }

        var items = await applications
            .OrderByDescending(application => application.UpdatedAtUtc)
            .ThenByDescending(application => application.Id)
            .Select(application => new ApplicationListItemViewModel
            {
                Id = application.Id,
                ApplicantName = isManager ? application.ApplicantInformation!.Name : string.Empty,
                PropertyName = application.Unit.Property.Name,
                UnitNumber = application.Unit.UnitNumber,
                Status = application.Status,
                UpdatedAtUtc = application.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new ApplicationListViewModel
        {
            IsManager = isManager,
            Filter = filter,
            Applications = items,
            Properties = await propertyOptions.OrderBy(property => property.Name)
                    .Select(property => new ApplicationPropertyOptionViewModel(property.Id, property.Name))
                    .ToListAsync(cancellationToken)
        };
    }

    public async Task<ApplicationDetailsViewModel?> GetDetailsAsync(
        int id,
        string userId,
        bool isManager,
        CancellationToken cancellationToken = default)
    {
        var applications = dbContext.RentalApplications
            .AsNoTracking()
            .Where(application => application.Id == id);
        if (!isManager)
        {
            applications = applications.Where(application => application.ApplicantId == userId);
        }

        var model = await applications.Select(application => new ApplicationDetailsViewModel
            {
                Id = application.Id,
                IsManager = isManager,
                PropertyName = application.Unit.Property.Name,
                PropertyAddress = application.Unit.Property.Address,
                UnitNumber = application.Unit.UnitNumber,
                UnitTypeName = application.Unit.UnitType.Name,
                Bedrooms = application.Unit.Bedrooms,
                MonthlyRent = application.Unit.MonthlyRent,
                Status = application.Status,
                CreatedAtUtc = application.CreatedAtUtc,
                ApplicantName = application.ApplicantInformation == null ? null : application.ApplicantInformation.Name,
                ApplicantPhone = application.ApplicantInformation == null ? null : application.ApplicantInformation.Phone,
                ApplicantEmail = application.ApplicantInformation == null ? null : application.ApplicantInformation.Email,
                CurrentAddress = application.ApplicantInformation == null ? null : application.ApplicantInformation.CurrentAddress,
                ReturnComment = application.Status == RentalApplicationStatus.Returned
                    ? application.Reviews.Where(review => review.Outcome == ReviewOutcome.Returned)
                        .OrderByDescending(review => review.CreatedAtUtc)
                        .Select(review => review.Comment)
                        .FirstOrDefault()
                    : null,
                LeaseStartDate = application.Lease == null ? null : application.Lease.StartDate,
                LeaseEndDate = application.Lease == null ? null : application.Lease.EndDate,
                Residences = application.Residences.OrderByDescending(residence => residence.MoveOutDate)
                    .Select(residence => new ResidenceItemViewModel
                    {
                        Address = residence.Address,
                        LandlordName = residence.LandlordName,
                        LandlordPhone = residence.LandlordPhone,
                        MoveInDate = residence.MoveInDate,
                        MoveOutDate = residence.MoveOutDate
                    }).ToList(),
                StatusHistory = application.StatusHistory.Where(history => isManager).OrderBy(history => history.CreatedAtUtc).ThenBy(history => history.Id)
                    .Select(history => new StatusHistoryItemViewModel
                    {
                        PreviousStatus = history.PreviousStatus,
                        NewStatus = history.NewStatus,
                        ChangedBy = history.ChangedByUser.Email!,
                        Comment = history.Comment,
                        ReviewOutcome = history.ReviewOutcome,
                        CreatedAtUtc = history.CreatedAtUtc
                    }).ToList(),
                Reviews = application.Reviews.Where(review => isManager).OrderByDescending(review => review.CreatedAtUtc)
                    .Select(review => new ReviewItemViewModel
                    {
                        Outcome = review.Outcome.ToString(),
                        Comment = review.Comment,
                        ReviewerEmail = review.Reviewer.Email!,
                        CreatedAtUtc = review.CreatedAtUtc
                    }).ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        return model;
    }
}

using PropertyRental.Web.Models.Enums;

namespace PropertyRental.Web.Models;

public class RentalApplication
{
    public int Id { get; set; }

    public int UnitId { get; set; }

    public required string ApplicantId { get; set; }

    public RentalApplicationStatus Status { get; set; }

    public ApplicationStep CurrentStep { get; set; }

    public bool ApplicantInformationSaved { get; set; }

    public bool ResidenceHistorySaved { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public DateTime? SubmittedAtUtc { get; set; }

    public Unit Unit { get; set; } = null!;

    public ApplicationUser Applicant { get; set; } = null!;

    public ApplicantInformation? ApplicantInformation { get; set; }

    public ICollection<Residence> Residences { get; } = [];

    public ICollection<ApplicationReview> Reviews { get; } = [];

    public ICollection<ApplicationStatusHistory> StatusHistory { get; } = [];

    public Lease? Lease { get; set; }
}

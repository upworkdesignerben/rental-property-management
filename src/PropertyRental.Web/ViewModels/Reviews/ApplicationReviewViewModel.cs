using System.ComponentModel.DataAnnotations;
using PropertyRental.Web.Models.Enums;

namespace PropertyRental.Web.ViewModels.Reviews;

public class ApplicationReviewViewModel : IValidatableObject
{
    public int ApplicationId { get; set; }
    [Required, EnumDataType(typeof(ReviewOutcome))]
    public ReviewOutcome? Outcome { get; set; }
    [StringLength(1000)]
    public string? Comment { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Outcome is ReviewOutcome.Returned or ReviewOutcome.Denied && string.IsNullOrWhiteSpace(Comment))
            yield return new ValidationResult("A comment is required for Return and Deny.", [nameof(Comment)]);
    }
}

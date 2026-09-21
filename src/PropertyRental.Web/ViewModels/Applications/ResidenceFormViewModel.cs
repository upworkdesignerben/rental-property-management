using System.ComponentModel.DataAnnotations;

namespace PropertyRental.Web.ViewModels.Applications;

public class ResidenceFormViewModel : IValidatableObject
{
    public int ApplicationId { get; set; }
    public int? ResidenceId { get; set; }
    [Required, StringLength(300)]
    public string Address { get; set; } = string.Empty;
    [Required, StringLength(160), Display(Name = "Landlord name")]
    public string LandlordName { get; set; } = string.Empty;
    [Required, StringLength(32), Phone, Display(Name = "Landlord phone")]
    public string LandlordPhone { get; set; } = string.Empty;
    [Required, DataType(DataType.Date), Display(Name = "Move-in date")]
    public DateOnly? MoveInDate { get; set; }
    [Required, DataType(DataType.Date), Display(Name = "Move-out date")]
    public DateOnly? MoveOutDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MoveOutDate < MoveInDate)
            yield return new ValidationResult("Move-out date must be on or after move-in date.", [nameof(MoveOutDate)]);
    }
}

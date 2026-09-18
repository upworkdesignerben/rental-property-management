using System.ComponentModel.DataAnnotations;

namespace PropertyRental.Web.ViewModels.Units;

public record class UnitFormViewModel
{
    public int? Id { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a property.")]
    [Display(Name = "Property")]
    public int PropertyId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a unit type.")]
    [Display(Name = "Unit type")]
    public int UnitTypeId { get; init; }

    [Required]
    [StringLength(32)]
    [Display(Name = "Unit number")]
    public string UnitNumber { get; init; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int Bedrooms { get; init; }

    [Range(typeof(decimal), "0.01", "99999999.99")]
    [Display(Name = "Monthly rent")]
    public decimal MonthlyRent { get; init; }

    public IReadOnlyList<UnitOptionViewModel> Properties { get; init; } = [];

    public IReadOnlyList<UnitOptionViewModel> UnitTypes { get; init; } = [];
}

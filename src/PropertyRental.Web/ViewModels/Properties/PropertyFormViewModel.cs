using System.ComponentModel.DataAnnotations;

namespace PropertyRental.Web.ViewModels.Properties;

public record class PropertyFormViewModel
{
    public int? Id { get; init; }

    [Required]
    [StringLength(160)]
    [Display(Name = "Property name")]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string Address { get; init; } = string.Empty;

    [Required]
    [StringLength(2_000)]
    public string Description { get; init; } = string.Empty;
}

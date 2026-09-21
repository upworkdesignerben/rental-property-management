using System.ComponentModel.DataAnnotations;

namespace PropertyRental.Web.ViewModels.Applications;

public class ApplicantInformationViewModel
{
    [Required, StringLength(160)]
    public string Name { get; set; } = string.Empty;
    [Required, StringLength(32), Phone]
    public string Phone { get; set; } = string.Empty;
    [Required, StringLength(256), EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required, StringLength(300), Display(Name = "Current address")]
    public string CurrentAddress { get; set; } = string.Empty;
    public bool IsReadOnly { get; set; }
}

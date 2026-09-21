namespace PropertyRental.Web.Models.Enums;

// Command sent by the pressed wizard button. Parsed from the posted
// "command" form value at the controller boundary; unknown values are
// rejected before reaching the service layer.
public enum WizardCommand
{
    Back,
    Continue,
    Submit
}

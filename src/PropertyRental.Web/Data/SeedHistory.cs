using System.ComponentModel.DataAnnotations;

namespace PropertyRental.Web.Data;

// A completed seed is independent of mutable demo names, statuses and user edits.
public class SeedHistory
{
    [Key, MaxLength(100)] public required string Name { get; set; }
    public DateTime AppliedAtUtc { get; set; }
}

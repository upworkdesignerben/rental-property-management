using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyRental.Web.Models;

namespace PropertyRental.Web.Data.Configurations;

public class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.Property(unit => unit.UnitNumber).HasMaxLength(32).IsRequired();
        builder.Property(unit => unit.MonthlyRent).HasPrecision(12, 2);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_Units_Bedrooms", "Bedrooms >= 0");
            table.HasCheckConstraint("CK_Units_MonthlyRent", "MonthlyRent > 0");
        });
        builder.HasIndex(unit => new { unit.PropertyId, unit.UnitNumber }).IsUnique();
    }
}

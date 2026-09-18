using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyRental.Web.Models;

namespace PropertyRental.Web.Data.Configurations;

public class UnitTypeConfiguration : IEntityTypeConfiguration<UnitType>
{
    public void Configure(EntityTypeBuilder<UnitType> builder)
    {
        builder.Property(unitType => unitType.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(unitType => unitType.Name).IsUnique();
        builder.HasMany(unitType => unitType.Units)
            .WithOne(unit => unit.UnitType)
            .HasForeignKey(unit => unit.UnitTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

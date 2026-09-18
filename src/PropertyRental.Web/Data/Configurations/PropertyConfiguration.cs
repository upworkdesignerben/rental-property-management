using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyRental.Web.Models;

namespace PropertyRental.Web.Data.Configurations;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.Property(property => property.Name).HasMaxLength(160).IsRequired();
        builder.Property(property => property.Address).HasMaxLength(300).IsRequired();
        builder.Property(property => property.Description).HasMaxLength(2_000).IsRequired();
        builder.HasMany(property => property.Units)
            .WithOne(unit => unit.Property)
            .HasForeignKey(unit => unit.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

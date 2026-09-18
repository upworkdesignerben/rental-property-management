using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyRental.Web.Models;

namespace PropertyRental.Web.Data.Configurations;

public class ResidenceConfiguration : IEntityTypeConfiguration<Residence>
{
    public void Configure(EntityTypeBuilder<Residence> builder)
    {
        builder.Property(residence => residence.Address).HasMaxLength(300).IsRequired();
        builder.Property(residence => residence.LandlordName).HasMaxLength(160).IsRequired();
        builder.Property(residence => residence.LandlordPhone).HasMaxLength(32).IsRequired();
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_Residences_MoveOutDate",
            "MoveOutDate >= MoveInDate"));
    }
}

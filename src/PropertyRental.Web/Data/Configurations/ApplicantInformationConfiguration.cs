using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyRental.Web.Models;

namespace PropertyRental.Web.Data.Configurations;

public class ApplicantInformationConfiguration : IEntityTypeConfiguration<ApplicantInformation>
{
    public void Configure(EntityTypeBuilder<ApplicantInformation> builder)
    {
        builder.HasKey(information => information.RentalApplicationId);
        builder.Property(information => information.Name).HasMaxLength(160).IsRequired();
        builder.Property(information => information.Phone).HasMaxLength(32).IsRequired();
        builder.Property(information => information.Email).HasMaxLength(256).IsRequired();
        builder.Property(information => information.CurrentAddress).HasMaxLength(300).IsRequired();
    }
}

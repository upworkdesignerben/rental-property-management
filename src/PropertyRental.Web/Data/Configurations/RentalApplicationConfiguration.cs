using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyRental.Web.Models;

namespace PropertyRental.Web.Data.Configurations;

public class RentalApplicationConfiguration : IEntityTypeConfiguration<RentalApplication>
{
    public void Configure(EntityTypeBuilder<RentalApplication> builder)
    {
        builder.Property(application => application.ApplicantId).HasMaxLength(450).IsRequired();
        builder.Property(application => application.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(application => application.CurrentStep).HasConversion<string>().HasMaxLength(32);
        builder.HasOne(application => application.Unit)
            .WithMany(unit => unit.RentalApplications)
            .HasForeignKey(application => application.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(application => application.Applicant)
            .WithMany()
            .HasForeignKey(application => application.ApplicantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(application => application.ApplicantInformation)
            .WithOne(information => information.RentalApplication)
            .HasForeignKey<ApplicantInformation>(information => information.RentalApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(application => application.Residences)
            .WithOne(residence => residence.RentalApplication)
            .HasForeignKey(residence => residence.RentalApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(application => application.Reviews)
            .WithOne(review => review.RentalApplication)
            .HasForeignKey(review => review.RentalApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(application => application.StatusHistory)
            .WithOne(history => history.RentalApplication)
            .HasForeignKey(history => history.RentalApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(application => application.Lease)
            .WithOne(lease => lease.Application)
            .HasForeignKey<Lease>(lease => lease.ApplicationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(application => new { application.ApplicantId, application.Status });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyRental.Web.Models;

namespace PropertyRental.Web.Data.Configurations;

public class ApplicationStatusHistoryConfiguration : IEntityTypeConfiguration<ApplicationStatusHistory>
{
    public void Configure(EntityTypeBuilder<ApplicationStatusHistory> builder)
    {
        builder.Property(history => history.ChangedByUserId).HasMaxLength(450).IsRequired();
        builder.Property(history => history.PreviousStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(history => history.NewStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(history => history.ReviewOutcome).HasConversion<string>().HasMaxLength(16);
        builder.Property(history => history.Comment).HasMaxLength(1_000);
        builder.HasOne(history => history.ChangedByUser)
            .WithMany()
            .HasForeignKey(history => history.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyRental.Web.Models;

namespace PropertyRental.Web.Data.Configurations;

public class ApplicationReviewConfiguration : IEntityTypeConfiguration<ApplicationReview>
{
    public void Configure(EntityTypeBuilder<ApplicationReview> builder)
    {
        builder.Property(review => review.ReviewerId).HasMaxLength(450).IsRequired();
        builder.Property(review => review.Outcome).HasConversion<string>().HasMaxLength(16);
        builder.Property(review => review.Comment).HasMaxLength(1_000);
        builder.HasOne(review => review.Reviewer)
            .WithMany()
            .HasForeignKey(review => review.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

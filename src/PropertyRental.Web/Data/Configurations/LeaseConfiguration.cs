using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyRental.Web.Models;

namespace PropertyRental.Web.Data.Configurations;

public class LeaseConfiguration : IEntityTypeConfiguration<Lease>
{
    public void Configure(EntityTypeBuilder<Lease> builder)
    {
        builder.HasIndex(lease => lease.ApplicationId).IsUnique();
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_Leases_EndDate",
            "EndDate >= StartDate"));
        builder.HasOne(lease => lease.Unit)
            .WithMany(unit => unit.Leases)
            .HasForeignKey(lease => lease.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using CountMeIn.Domain.Checks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CountMeIn.Infrastructure.Persistence.Configurations;

public class InterestCheckConfiguration : IEntityTypeConfiguration<InterestCheck>
{
    public void Configure(EntityTypeBuilder<InterestCheck> b)
    {
        b.ToTable("interest_checks");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever(); // ids are assigned by the domain; EF must treat children found via navigation as new
        b.Property(x => x.ShareId).HasMaxLength(32).IsRequired();
        b.HasIndex(x => x.ShareId).IsUnique();
        b.Property(x => x.OwnerKeyHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Pitch).HasMaxLength(4000).IsRequired();
        b.Property(x => x.Venue).HasMaxLength(500);
        b.Property<int>("NextQueueNumber");
        b.Property(x => x.Version).IsConcurrencyToken();

        // Only unreleased checks with a window are interesting to the release job.
        b.HasIndex(x => x.ReleaseAt).HasFilter("\"SpotsReleasedAt\" IS NULL AND \"ReleaseAt\" IS NOT NULL");

        b.Ignore(x => x.Count);
        b.Ignore(x => x.SpotsHeld);
        b.Ignore(x => x.WaitlistCount);
        b.Ignore(x => x.GoalReached);
        b.Ignore(x => x.SpotsLeft);
        b.Ignore(x => x.HasWindow);

        b.HasMany(x => x.Participants).WithOne().HasForeignKey(p => p.CheckId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Participants).HasField("_participants").UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasMany(x => x.Changes).WithOne().HasForeignKey(p => p.CheckId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Changes).HasField("_changes").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

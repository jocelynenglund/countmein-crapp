using CountMeIn.Domain.Checks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CountMeIn.Infrastructure.Persistence.Configurations;

public class ParticipantConfiguration : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> b)
    {
        b.ToTable("participants");
        b.HasKey(x => x.Id);
        b.Property(x => x.PersonId).HasMaxLength(100).IsRequired();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Email).HasMaxLength(320);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(x => new { x.CheckId, x.PersonId }).IsUnique();
        b.HasIndex(x => new { x.CheckId, x.Status, x.QueueNumber });
        b.Ignore(x => x.IsCountedIn);
        b.Ignore(x => x.HoldsASpot);
        b.Ignore(x => x.IsWaitlisted);
        b.Ignore(x => x.IsEarlyBird);
    }
}

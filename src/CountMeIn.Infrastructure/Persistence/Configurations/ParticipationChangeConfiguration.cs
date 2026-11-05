using CountMeIn.Domain.Checks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CountMeIn.Infrastructure.Persistence.Configurations;

public class ParticipationChangeConfiguration : IEntityTypeConfiguration<ParticipationChange>
{
    public void Configure(EntityTypeBuilder<ParticipationChange> b)
    {
        b.ToTable("participation_changes");
        b.HasKey(x => x.Id);
        b.Property(x => x.PersonId).HasMaxLength(100).IsRequired();
        b.Property(x => x.Kind).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(x => new { x.CheckId, x.Day });
    }
}

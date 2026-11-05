using CountMeIn.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CountMeIn.Infrastructure.Persistence;

public class InterestCheckConfiguration : IEntityTypeConfiguration<InterestCheck>
{
    public void Configure(EntityTypeBuilder<InterestCheck> builder)
    {
        builder.ToTable("interest_checks");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasMaxLength(Identifiers.MaxLength).ValueGeneratedNever();
        builder.Property(c => c.Title).HasMaxLength(InterestCheck.MaxTitleLength).IsRequired();
        builder.Property(c => c.Pitch).HasMaxLength(InterestCheck.MaxPitchLength).IsRequired();
        builder.Property(c => c.Venue).HasMaxLength(InterestCheck.MaxVenueLength);
        builder.Property(c => c.OwnerKeyHash).HasMaxLength(64).IsRequired();

        // The aggregate exposes a read-only view; EF fills the private list.
        builder.HasMany(c => c.Participants)
            .WithOne()
            .HasForeignKey("InterestCheckId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(c => c.ChangedMinds)
            .WithOne()
            .HasForeignKey("InterestCheckId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.ChangedMinds).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(c => c.Participants).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Ignore(c => c.Count);
        builder.Ignore(c => c.GoalReached);
        builder.Ignore(c => c.SpotsLeft);
        builder.Ignore(c => c.WaitlistCount);

        // Computed views over Participants, not relationships.
        builder.Ignore(c => c.InSignUpOrder);
        builder.Ignore(c => c.SpotHolders);
        builder.Ignore(c => c.Waitlist);
        builder.Ignore(c => c.EarlyBirds);
        builder.Ignore(c => c.LetInFromWaitlist);
        builder.Ignore(c => c.LostTheirSpot);
        builder.Ignore(c => c.ReleaseAt);
    }
}

public class ParticipantConfiguration : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> builder)
    {
        builder.ToTable("participants");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.PersonId).HasMaxLength(Identifiers.MaxLength).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(Participant.MaxNameLength).IsRequired();
        builder.Property(p => p.Email).HasMaxLength(Participant.MaxEmailLength);

        // The same person can only be in a check once, even if two requests race.
        builder.HasIndex("InterestCheckId", nameof(Participant.PersonId)).IsUnique();
    }
}

public class ChangedMindConfiguration : IEntityTypeConfiguration<ChangedMind>
{
    public void Configure(EntityTypeBuilder<ChangedMind> builder)
    {
        builder.ToTable("changed_minds");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.PersonId).HasMaxLength(Identifiers.MaxLength).IsRequired();
        builder.HasIndex("InterestCheckId", nameof(ChangedMind.PersonId)).IsUnique();
    }
}

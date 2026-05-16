using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MemeMayhem.Core.Entities;

namespace MemeMayhem.Infrastructure.Data.Configurations;

public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nickname)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(p => p.ConnectionId)
            .HasMaxLength(100);

        // Votes relationship — restrict delete to avoid
        // cascade conflict with CardPlay
        builder.HasMany(p => p.Votes)
            .WithOne(v => v.Voter)
            .HasForeignKey(v => v.VoterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.CardPlays)
            .WithOne(cp => cp.Player)
            .HasForeignKey(cp => cp.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
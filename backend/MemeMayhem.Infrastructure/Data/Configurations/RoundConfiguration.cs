// RoundConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MemeMayhem.Core.Entities;

namespace MemeMayhem.Infrastructure.Data.Configurations;

public class RoundConfiguration : IEntityTypeConfiguration<Round>
{
    public void Configure(EntityTypeBuilder<Round> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.PromptText)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(r => r.Status)
            .HasConversion<string>();

        builder.Property(r => r.TurnOrder)
            .HasMaxLength(1000);            // JSON array of GUIDs

        builder.HasMany(r => r.CardPlays)
            .WithOne(cp => cp.Round)
            .HasForeignKey(cp => cp.RoundId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
// VoteConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MemeMayhem.Core.Entities;

namespace MemeMayhem.Infrastructure.Data.Configurations;

public class VoteConfiguration : IEntityTypeConfiguration<Vote>
{
    public void Configure(EntityTypeBuilder<Vote> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.VoteType)
            .HasConversion<string>();

        // One vote per voter per card play — no double voting
        builder.HasIndex(v => new { v.CardPlayId, v.VoterId })
            .IsUnique();
    }
}
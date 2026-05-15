// RoomConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MemeMayhem.Core.Entities;

namespace MemeMayhem.Infrastructure.Data.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code)
            .IsRequired()
            .HasMaxLength(6);

        builder.HasIndex(r => r.Code)
            .IsUnique();                    // codes must be unique

        builder.Property(r => r.Theme)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.Status)
            .HasConversion<string>();       // store as string not int

        builder.HasMany(r => r.Players)
            .WithOne(p => p.Room)
            .HasForeignKey(p => p.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Rounds)
            .WithOne(r => r.Room)
            .HasForeignKey(r => r.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
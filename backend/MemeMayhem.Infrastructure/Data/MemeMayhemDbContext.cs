using Microsoft.EntityFrameworkCore;
using MemeMayhem.Core.Entities;

namespace MemeMayhem.Infrastructure.Data;

public class MemeMayhemDbContext : DbContext
{
    public MemeMayhemDbContext(DbContextOptions<MemeMayhemDbContext> options)
        : base(options) { }

    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<MemeCard> MemeCards => Set<MemeCard>();
    public DbSet<PlayerCard> PlayerCards => Set<PlayerCard>();
    public DbSet<ReactionGif> ReactionGifs => Set<ReactionGif>();
    public DbSet<Round> Rounds => Set<Round>();
    public DbSet<CardPlay> CardPlays => Set<CardPlay>();
    public DbSet<Vote> Votes => Set<Vote>();
    public DbSet<RoundScore> RoundScores => Set<RoundScore>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(MemeMayhemDbContext).Assembly
        );
    }
}
using MemeMayhem.Core.Enums;

namespace MemeMayhem.Core.Entities;

public class Vote
{
    public Guid Id { get; set; }
    public Guid CardPlayId { get; set; }
    public Guid VoterId { get; set; }
    public VoteType VoteType { get; set; }
    public int Points { get; set; }
    public DateTime VotedAt { get; set; } = DateTime.UtcNow;

    public CardPlay CardPlay { get; set; } = null!;
    public Player Voter { get; set; } = null!;
}
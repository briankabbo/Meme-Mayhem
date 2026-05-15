using MemeMayhem.Core.Enums;
namespace MemeMayhem.Core.Entities;

public class Round
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public int RoundNumber { get; set; }
    public string PromptText { get; set; } = string.Empty;
    public RoundStatus Status { get; set; } = RoundStatus.CardPicking;
    public string TurnOrder { get; set; } = "[]";
    public int CurrentTurnIndex { get; set; } = 0;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }

    public Room Room { get; set; } = null!;
    public ICollection<CardPlay> CardPlays { get; set; } = new List<CardPlay>();
    public ICollection<RoundScore> RoundScores { get; set; } = new List<RoundScore>();
}
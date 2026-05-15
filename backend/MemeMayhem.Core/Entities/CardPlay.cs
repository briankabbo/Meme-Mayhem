namespace MemeMayhem.Core.Entities;

public class CardPlay
{
    public Guid Id { get; set; }
    public Guid RoundId { get; set; }
    public Guid PlayerId { get; set; }
    public Guid MemeCardId { get; set; }
    public int TurnIndex { get; set; }
    public DateTime PlayedAt { get; set; } = DateTime.UtcNow;

    public Round Round { get; set; } = null!;
    public Player Player { get; set; } = null!;
    public MemeCard MemeCard { get; set; } = null!;
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
}
// PlayerCard.cs — player's hand
namespace MemeMayhem.Core.Entities;

public class PlayerCard
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Guid MemeCardId { get; set; }
    public Guid RoomId { get; set; }        // scoped to room session
    public bool IsPlayed { get; set; } = false;
    public DateTime DealtAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Player Player { get; set; } = null!;
    public MemeCard MemeCard { get; set; } = null!;
}
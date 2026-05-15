using MemeMayhem.Core.Enums;
namespace MemeMayhem.Core.Entities;

public class Room
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;    // 6-char join code
    public Guid HostPlayerId { get; set; }
    public string Theme { get; set; } = string.Empty;
    public RoomStatus Status { get; set; } = RoomStatus.Lobby;
    public int TotalRounds { get; set; }
    public int CurrentRound { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Player> Players { get; set; } = new List<Player>();
    public ICollection<Round> Rounds { get; set; } = new List<Round>();
}
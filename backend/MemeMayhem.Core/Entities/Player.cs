using MemeMayhem.Core.Enums;
namespace MemeMayhem.Core.Entities;

public class Player
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public bool IsHost { get; set; } = false;
    public bool IsSpectator { get; set; } = false;
    public bool IsConnected { get; set; } = true;
    public int TotalScore { get; set; } = 0;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DisconnectedAt { get; set; }

    public Room Room { get; set; } = null!;
    public ICollection<PlayerCard> PlayerCards { get; set; } = new List<PlayerCard>();
    public ICollection<CardPlay> CardPlays { get; set; } = new List<CardPlay>();
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
    public ICollection<RoundScore> RoundScores { get; set; } = new List<RoundScore>();
}
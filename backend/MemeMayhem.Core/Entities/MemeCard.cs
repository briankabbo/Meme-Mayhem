using MemeMayhem.Core.Enums;
namespace MemeMayhem.Core.Entities;

public class MemeCard
{
    public Guid Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;  // Imgflip/Tenor ID
    public string Label { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;    // Azure Blob URL
    public CardSource Source { get; set; }
    public string Tags { get; set; } = "[]";                // JSON array
    public bool IsNsfw { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<PlayerCard> PlayerCards { get; set; } = new List<PlayerCard>();
    public ICollection<CardPlay> CardPlays { get; set; } = new List<CardPlay>();
}
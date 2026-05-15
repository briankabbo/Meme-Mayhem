using MemeMayhem.Core.Enums;

namespace MemeMayhem.Core.Entities;

public class ReactionGif
{
    public Guid Id { get; set; }
    public VoteType VoteType { get; set; }
    public string GifUrl { get; set; } = string.Empty;
    public string TenorId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
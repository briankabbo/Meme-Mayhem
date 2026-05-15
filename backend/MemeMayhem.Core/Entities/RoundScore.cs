using MemeMayhem.Core.Enums;
namespace MemeMayhem.Core.Entities;
public class RoundScore
{
    public Guid Id { get; set; }
    public Guid RoundId { get; set; }
    public Guid PlayerId { get; set; }
    public int PointsEarned { get; set; }   // points this round
    public int RunningTotal { get; set; }   // cumulative score

    // Navigation
    public Round Round { get; set; } = null!;
    public Player Player { get; set; } = null!;
}
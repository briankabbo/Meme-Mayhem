namespace MemeMayhem.Core.DTOs;

public class PlayerScoreDto
{
    public Guid PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int PointsEarned { get; set; }    // points from this round only
    public int RunningTotal { get; set; }    // cumulative score
    public int Rank { get; set; }            // position on leaderboard
}
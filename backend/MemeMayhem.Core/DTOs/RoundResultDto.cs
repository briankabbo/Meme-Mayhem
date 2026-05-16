namespace MemeMayhem.Core.DTOs;

public class RoundResultDto
{
    public Guid RoundId { get; set; }
    public int RoundNumber { get; set; }
    public int TotalRounds { get; set; }
    public bool IsGameOver { get; set; }
    public List<PlayerScoreDto> Scores { get; set; } = new();
}
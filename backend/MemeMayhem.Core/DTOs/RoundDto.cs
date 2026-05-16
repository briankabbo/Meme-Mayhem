namespace MemeMayhem.Core.DTOs;

public class RoundDto
{
    public Guid Id { get; set; }
    public int RoundNumber { get; set; }
    public string PromptText { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid CurrentPlayerId { get; set; }
    public int CurrentTurnIndex { get; set; }
    public int TotalTurns { get; set; }
    public List<CardPlayDto> CardPlays { get; set; } = new();
}
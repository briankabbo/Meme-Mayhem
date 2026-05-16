namespace MemeMayhem.Core.DTOs;

public class RoomDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Theme { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalRounds { get; set; }
    public int CurrentRound { get; set; }
    public List<PlayerDto> Players { get; set; } = new();
}
namespace MemeMayhem.Core.DTOs;

public class PlayerDto
{
    public Guid Id { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public bool IsHost { get; set; }
    public bool IsSpectator { get; set; }
    public bool IsConnected { get; set; }
    public int TotalScore { get; set; }
}
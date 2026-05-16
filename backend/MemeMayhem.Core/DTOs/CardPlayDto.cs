namespace MemeMayhem.Core.DTOs;

public class CardPlayDto
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public MemeCardDto Card { get; set; } = null!;
    public int TurnIndex { get; set; }
    public List<VoteDto> Votes { get; set; } = new();
}
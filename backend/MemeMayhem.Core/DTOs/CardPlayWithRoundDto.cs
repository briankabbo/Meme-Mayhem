namespace MemeMayhem.Core.DTOs;

public class CardPlayWithRoundDto
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public RoundDto Round { get; set; } = null!;
}
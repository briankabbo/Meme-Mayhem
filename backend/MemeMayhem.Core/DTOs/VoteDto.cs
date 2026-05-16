namespace MemeMayhem.Core.DTOs;

public class VoteDto
{
    public Guid VoterId { get; set; }
    public string VoterName { get; set; } = string.Empty;
    public string VoteType { get; set; } = string.Empty;
    public int Points { get; set; }
}
namespace MemeMayhem.Core.DTOs;

public class MemeCardDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}
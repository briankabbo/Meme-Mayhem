namespace MemeMayhem.Core.Interfaces;

public interface IAIPromptService
{
    Task<string> GeneratePromptAsync(string theme, List<string>? usedPrompts = null);
}
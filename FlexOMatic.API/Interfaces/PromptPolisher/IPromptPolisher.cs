public interface IPromptPolisher
{
    Task<(string polishedPrompt, string cropName)> PolishAsync(string userPrompt);
}
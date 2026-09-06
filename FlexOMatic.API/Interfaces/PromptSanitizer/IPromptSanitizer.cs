public interface IPromptSanitizer
{
    Task<string> SanitizeAsync(string rawPrompt);
}

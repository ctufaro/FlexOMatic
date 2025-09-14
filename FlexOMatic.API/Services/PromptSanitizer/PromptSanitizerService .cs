using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;

public class PromptSanitizerService : IPromptSanitizer
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    private static readonly string[] BannedWords = {
        "fuck", "shit", "bitch", "asshole", "kill", "nazi", "porn", "dumb", "pedo", "terror", "rape"
    };

    public PromptSanitizerService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _apiKey = config["OpenAI:ApiKey"];
    }

    public async Task<string> SanitizeAsync(string rawPrompt)
    {
        if (string.IsNullOrWhiteSpace(rawPrompt))
            return "mystery crop";

        // Profanity filter — immediate redirect
        var lowered = rawPrompt.ToLowerInvariant();
        foreach (var word in BannedWords)
        {
            if (lowered.Contains(word))
                return "angry emoji crop";
        }

        // Construct OpenAI prompt to rewrite the crop idea
        var payload = new
        {
            model = "gpt-4o",
            temperature = 0.6,
            messages = new[]
            {
                new {
                    role = "system",
                    content = @"You are a creative assistant for a kids' Roblox-style game called Flex Farm. Your job is to take messy, weird, or inappropriate crop ideas and turn them into short, fun, family-safe crop names — like 'angry cactus crop' or 'bubblegum soda plant'. Never return paragraphs, marketing language, or explanations. Keep it simple and under 5 words. No profanity or adult content. Just a clean, silly crop name."
                },
                new {
                    role = "user",
                    content = $"Turn this idea into something fun and appropriate for kids: {rawPrompt}"
                }
            }
        };

        // Send request to OpenAI
        var requestContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", requestContent);
        response.EnsureSuccessStatusCode();

        var resultJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(resultJson);

        var output = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()?
            .Trim() ?? "mystery crop";

        // Post-cleanup: strip punctuation/sentences
        output = output.Split(new[] { '.', '!', '?', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();

        // Enforce crop ending if not already there
        if (!output.ToLower().Contains("crop"))
        {
            output += " crop";
        }

        // Force length limit
        if (output.Length > 60)
        {
            output = output.Substring(0, 60).Trim();
        }

        return output;
    }
}

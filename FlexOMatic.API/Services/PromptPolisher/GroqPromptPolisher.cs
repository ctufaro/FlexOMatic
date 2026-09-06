using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

public class GroqPromptPolisher : IPromptPolisher
{
    private readonly HttpClient _client;
    private readonly string _apiKey;

    public GroqPromptPolisher(HttpClient client, IConfiguration config)
    {
        _client = client;
        _apiKey = config["Groq:ApiKey"];
    }

    public async Task<(string polishedPrompt, string cropName)> PolishAsync(string userPrompt)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var systemPrompt = @"An ultra low-poly crop shaped exactly like a blocky user request, simplified into chunky, geometric shapes with exaggerated proportions. Construct it using only basic forms: rectangles, triangles, and cubes—absolutely no curves, rounded edges, textures, or shading. Add a few playful, thematic details related to the object, keeping them extremely simple and angular. The crop grows on a rectangular green stalk with exactly two angular polygon leaves sprouting symmetrically from its sides, emerging from a simple cube of plain brown soil. Render with flat, bright colors only. Scene centered against a pure white background, perfectly matching the whimsical and humorous Roblox-inspired style.

Now, based on the user's idea, return TWO things:
1. A vivid 1000-character prompt that follows the visual style above.
2. A clever, short crop name (max 5 words) suitable for kids in a Roblox-style farming game.

Respond in this format:
---
PROMPT: <the polished image prompt>
NAME: <the clever crop name>";

        var payload = new
        {
            model = "llama3-70b-8192",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.7
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        var fullOutput = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(fullOutput))
            throw new Exception("Groq response was empty");

        string prompt = ExtractBetween(fullOutput, "PROMPT:", "NAME:")?.Trim();
        string name = ExtractAfter(fullOutput, "NAME:")?.Trim();

        if (string.IsNullOrWhiteSpace(prompt) || string.IsNullOrWhiteSpace(name))
            throw new Exception("Could not extract prompt or name from Groq response.");

        return (prompt.Length > 1000 ? prompt.Substring(0, 1000).Trim() : prompt, name);
    }

    private static string? ExtractBetween(string input, string start, string end)
    {
        var startIdx = input.IndexOf(start);
        var endIdx = input.IndexOf(end);
        if (startIdx == -1 || endIdx == -1 || endIdx <= startIdx) return null;
        return input.Substring(startIdx + start.Length, endIdx - startIdx - start.Length);
    }

    private static string? ExtractAfter(string input, string marker)
    {
        var idx = input.IndexOf(marker);
        if (idx == -1) return null;
        return input.Substring(idx + marker.Length);
    }
}

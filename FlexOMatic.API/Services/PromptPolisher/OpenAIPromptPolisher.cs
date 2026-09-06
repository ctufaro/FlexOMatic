using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq; // for Any/FirstOrDefault

public class OpenAIPromptPolisher : IPromptPolisher
{
    private readonly HttpClient _client;
    private readonly string _apiKey;
    private readonly string _model;

    // For JsonSerializer (serialize payloads, optional future deserializations)
    private static readonly JsonSerializerOptions SerializerOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
        // NOTE: AllowTrailingCommas is NOT a JsonSerializerOptions property.
    };

    // For JsonDocument.Parse(...) only
    private static readonly JsonDocumentOptions DocOpts = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        MaxDepth = 0 // 0 = default / unlimited practical depth
    };

    private static readonly string[] DisallowedBrandHints = new[]
    {   
        ""
        //"nike","adidas","youtube","mario","pokemon","lego","apple","google","disney",
        //"xbox","playstation","ps5","minecraft","fortnite","tiktok","instagram","facebook",
        //"nintendo","microsoft","sony","warner","marvel","dc","star wars","harry potter"
    };

    public OpenAIPromptPolisher(HttpClient client, IConfiguration config)
    {
        _client = client;
        _apiKey = config["OpenAI:ApiKey"];
        _model = config["OpenAI:PolisherModel"] ?? "gpt-3.5-turbo";
    }

    public async Task<(string polishedPrompt, string cropName)> PolishAsync(string userPrompt)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var systemPrompt = """
You are the Flex Farm Prompt Polisher.

GOAL
Turn the user's idea into a clean image-generation prompt for an ultra low-poly, object-only item suitable for a Roblox-style game icon. The final image shows ONLY the object (no people, no text), centered, transparent background.

STYLE RULES (STRICT)
- Ultra low-poly, chunky, geometric, exaggerated proportions.
- Use only simple prisms: rectangles, triangles, cubes. Avoid curves/rounded edges.
- Flat, bright, solid colors only. No textures, no gradients, no outlines, no shadows, no reflections, no glow/halo.
- Single object, centered. No stand, no ground, no soil, no stalk, no leaves, no scene. Transparent background.

SUBJECT RULES
- If request references a real person, fictional character, brand, logo, or recognizable IP:
  - Do NOT depict the person/character/logo or any protected mark.
  - INSTEAD: produce a symbolic, generic, geometric interpretation using 2–3 generic motifs of the category.
  - Keep motifs generic (no trademark shapes or specific colorways).
- If the request is unsafe/NSFW/violent/weapon, reinterpret into a playful, non-violent, toy-like geometric prop.

RENDER / NEGATIVES
- Object only; centered; transparent background.
- Add at end: NEGATIVE PROMPT forbidding text, logos, watermarks, gradients, outlines, reflections, shadows, glow/halos, realistic textures, photorealism, specific character likeness, brand marks.

NAMING
- Generate a short, clever, family-friendly crop name (no brand names or real names).

OUTPUT FORMAT (STRICT)
Return valid JSON ONLY:
{
  "polished_prompt": "<final image prompt>",
  "crop_name": "<clever short name>"
}
""";

        // First attempt
        var (ok, polished, name, rawContent) = await CallOnceAsync(systemPrompt, userPrompt);
        if (!ok)
        {
            (ok, polished, name, rawContent) = await CallOnceAsync(
                systemPrompt + "\n\nIMPORTANT: If previous attempt failed, ensure the response is valid JSON with only the keys polished_prompt and crop_name.",
                userPrompt);
        }

        // Guardrail for brands
        if (ok && ContainsBrandHint(polished))
        {
            var guardrailUser = $"{userPrompt}\n\nPlease genericize and remove brands or specific names. Use only generic symbolic motifs.";
            (ok, polished, name, rawContent) = await CallOnceAsync(
                systemPrompt + "\n\nAdditional rule: remove brand names and convert to generic/symbolic geometric motifs.",
                guardrailUser);
        }

        if (!ok)
        {
            var (polishedFallback, nameFallback) = ParseLegacyLines(rawContent);
            if (!string.IsNullOrWhiteSpace(polishedFallback) && !string.IsNullOrWhiteSpace(nameFallback))
                return (polishedFallback, nameFallback);

            return ("Ultra low-poly geometric object; object only; centered; transparent background; flat bright colors; no gradients, no outlines, no shadows. NEGATIVE PROMPT: text, logos, watermarks, brand marks, gradients, outlines, reflections, shadows, glow, halos, realistic textures, photorealism.", "Mystery Chunk");
        }

        return (polished, name);
    }

    private async Task<(bool ok, string polished, string name, string rawContent)> CallOnceAsync(string systemPrompt, string userPrompt)
    {
        var payload = new
        {
            model = _model,
            temperature = 0.6,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "system", content = "Reply with valid JSON only. Do not include markdown fences." },
                new { role = "user", content = userPrompt }
            }
        };

        using var content = new StringContent(JsonSerializer.Serialize(payload, SerializerOpts), Encoding.UTF8, "application/json");
        using var response = await _client.PostAsync("https://api.openai.com/v1/chat/completions", content);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return (false, "", "", json);

        using var doc = JsonDocument.Parse(json, DocOpts);
        var fullContent = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        if (TryParseAssistantJson(fullContent, out var polished, out var name))
            return (true, polished, name, fullContent);

        return (false, "", "", fullContent);
    }

    private static bool TryParseAssistantJson(string content, out string polished, out string name)
    {
        polished = "";
        name = "";

        if (string.IsNullOrWhiteSpace(content))
            return false;

        var trimmed = content.Trim();

        // Strip code fences if present
        if (trimmed.StartsWith("```"))
        {
            trimmed = Regex.Replace(trimmed, "^```[a-zA-Z]*\\s*", "");
            trimmed = Regex.Replace(trimmed, "\\s*```\\s*$", "");
        }

        try
        {
            using var j = JsonDocument.Parse(trimmed, DocOpts);
            if (j.RootElement.TryGetProperty("polished_prompt", out var pp))
                polished = pp.GetString() ?? "";
            if (j.RootElement.TryGetProperty("crop_name", out var cn))
                name = cn.GetString() ?? "";

            return !string.IsNullOrWhiteSpace(polished) && !string.IsNullOrWhiteSpace(name);
        }
        catch
        {
            return false;
        }
    }

    private static (string polished, string name) ParseLegacyLines(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return ("", "");
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        string polished = lines.FirstOrDefault(l => l.StartsWith("PolishedPrompt:", StringComparison.OrdinalIgnoreCase))?
            .Replace("PolishedPrompt:", "", StringComparison.OrdinalIgnoreCase).Trim() ?? "";
        string name = lines.FirstOrDefault(l => l.StartsWith("CropName:", StringComparison.OrdinalIgnoreCase))?
            .Replace("CropName:", "", StringComparison.OrdinalIgnoreCase).Trim() ?? "";
        return (polished, name);
    }

    private static bool ContainsBrandHint(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var lower = text.ToLowerInvariant();
        return DisallowedBrandHints.Any(h => lower.Contains(h));
    }
}

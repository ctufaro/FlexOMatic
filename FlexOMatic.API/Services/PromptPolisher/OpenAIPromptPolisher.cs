using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

public class OpenAIPromptPolisher : IPromptPolisher
{
    private readonly HttpClient _client;
    private readonly string _apiKey;

    public OpenAIPromptPolisher(HttpClient client, IConfiguration config)
    {
        _client = client;
        _apiKey = config["OpenAI:ApiKey"];
    }

    public async Task<(string polishedPrompt, string cropName)> PolishAsync(string userPrompt)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var systemPrompt = @"An ultra low-poly crop shaped exactly like the user’s requested object — or, if the request is a person, character, brand, or abstract concept, transform it into a safe, symbolic geometric design using 2–3 iconic, easily recognized motifs (no faces, no logos). Simplify into chunky, geometric shapes with exaggerated proportions, using only rectangles, triangles, and cubes — absolutely no curves, rounded edges, textures, or shading. Add a few playful, thematic details related to the idea, keeping them extremely simple and angular. The crop grows on a rectangular green stalk with exactly two angular polygon leaves sprouting symmetrically from its sides, emerging from a simple cube of plain brown soil. Render with flat, bright colors only. Scene centered against a pure white background, perfectly matching the whimsical and humorous Roblox-inspired style. Remove the background completely and make it transparent. Absolutely no shadows, drop shadows, cast shadows, outlines, gradients, or halo effects — every element must be fully flat with solid colors only.

        Design the subject for effortless voxel or block-model conversion: describe a straight-on, front-facing orthographic view with a perfectly upright orientation. Limit the palette to 4–6 solid colors with sharp separations and no semi-transparency. Keep silhouettes chunky with only 90-degree or 45-degree angles, broad flat faces, and large color regions that can extrude cleanly into 3D. Avoid thin details, floating particles, lighting effects, textures, noise, or soft edges. Make sure any accessories or motifs attach directly to the main form as sturdy blocky components so they can be modeled as simple boxes or wedges.";

        After polishing the prompt, generate a **clever, short crop name** that players will see in the game. It should be funny, imaginative, and match the vibe of the image prompt, avoiding real names and trademarks.

        Respond in this format only:

        PolishedPrompt: <your polished prompt>
        CropName: <your creative crop name>";

        var payload = new
        {
            model = "gpt-3.5-turbo",
            temperature = 0.7,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("https://api.openai.com/v1/chat/completions", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var fullContent = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        var lines = fullContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var polished = lines.FirstOrDefault(l => l.StartsWith("PolishedPrompt:"))?.Replace("PolishedPrompt:", "").Trim();
        var name = lines.FirstOrDefault(l => l.StartsWith("CropName:"))?.Replace("CropName:", "").Trim();

        return (polished ?? "", name ?? "🌱 Mystery Crop");
    }

}

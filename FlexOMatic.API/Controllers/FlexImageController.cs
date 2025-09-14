using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;

[ApiController]
[Route("api/[controller]")]
public class FlexImageController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly IPromptSanitizer _promptSanitizer;
    private readonly IPromptPolisher _promptPolisher;
    private readonly AzureBlobUploader _blobUploader;
    private readonly FlexCropRepository _repo;

    public FlexImageController(HttpClient httpClient, IConfiguration config,
        IPromptSanitizer promptSanitizer, IPromptPolisher promptPolisher,
        AzureBlobUploader blobUploader, FlexCropRepository repo)
    {
        _httpClient = httpClient;
        _config = config;
        _promptSanitizer = promptSanitizer;
        _promptPolisher = promptPolisher;
        _blobUploader = blobUploader;
        _repo = repo;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateImage([FromBody] CropRequest request)
    {
        if (!_config.GetValue<bool>("EnableSubmit"))
            return StatusCode(503, new { error = "Submissions are temporarily disabled." });

        if (string.IsNullOrWhiteSpace(request.CropIdea))
            return BadRequest("Crop idea cannot be empty.");

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var count = _repo.CountSubmissionsFromIp(ip, DateTime.UtcNow.AddHours(-24));
        var maxSubmissionsPerDay = _config.GetValue<int?>("MaxSubmissionsPerDay");
        if (count >= maxSubmissionsPerDay)
            return BadRequest($"Submission limit reached ({maxSubmissionsPerDay} per 24 hours)");

        var safePrompt = request.CropIdea;
        var (polishedPrompt, cropName) = await _promptPolisher.PolishAsync(safePrompt);

        // Explicitly request a shadowless render to avoid faint drop shadows
        polishedPrompt += " Rendered with flat colors and no shadows or drop shadows.";

        var payload = new
        {
            model = "gpt-image-1",
            prompt = polishedPrompt,
            n = 1,
            size = "1024x1024",
            quality = "low",
            background = "transparent"
        };

        var json = JsonSerializer.Serialize(payload);
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/images/generations")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config["OpenAI:ApiKey"]);

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var resp = await _httpClient.SendAsync(requestMessage, cts.Token);
        var respJson = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            _repo.LogRequest(HttpContext.Connection.RemoteIpAddress?.ToString(), request.CropIdea, polishedPrompt, (int)resp.StatusCode, "Image generation failed:"+respJson);
            return StatusCode((int)resp.StatusCode, new
            {
                error = "Image generation failed",
                status = resp.StatusCode,
                details = respJson
            });
        }

        using var doc = JsonDocument.Parse(respJson);
        var b64 = doc.RootElement.GetProperty("data")[0].GetProperty("b64_json").GetString();
        byte[] imageBytes = Convert.FromBase64String(b64);
        string fileName = $"flexcrop-{Guid.NewGuid()}.png";
        string imageUrl = await _blobUploader.UploadCropImageAsync(imageBytes, fileName);


        var newCropId = _repo.InsertMinimalReturningId(request.CropIdea, cropName, imageUrl); // INSERT INTO DB
        _repo.LogRequest(HttpContext.Connection.RemoteIpAddress?.ToString(), request.CropIdea, polishedPrompt, 200, "Success");


        return Ok(new
        {
            cropId = newCropId,
            imageUrl,
            originalIdea = request.CropIdea,
            polishedPrompt
        });
    }

}

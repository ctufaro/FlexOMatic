using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")] // -> /api/flexcrop
public class FlexCropController : ControllerBase
{
    private readonly FlexCropRepository _repo;
    private readonly IPromptPolisher _promptPolisher;
    private readonly AzureBlobUploader _blobUploader;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public FlexCropController(
        FlexCropRepository repo,
        IPromptPolisher polisher,
        AzureBlobUploader blobUploader,
        IHttpClientFactory clientFactory,
        IConfiguration config)
    {
        _repo = repo;
        _promptPolisher = polisher;
        _blobUploader = blobUploader;
        _httpClient = clientFactory.CreateClient();
        _config = config;
    }

    [HttpGet] // GET /api/flexcrop
    public ActionResult<IEnumerable<FlexCrop>> GetAll()
    {
        var crops = _repo.GetAll();
        return Ok(crops);
    }

    // POST /api/flexcrop/126/credit
    [HttpPost("{id:int}/credit")]
    public IActionResult CreditCrop([FromRoute] int id, [FromBody] CreditRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SubmitterName))
            return BadRequest("SubmitterName is required");

        _repo.UpdateSubmitterName(id, request.SubmitterName.Trim());
        return Ok();
    }

    // POST /api/flexcrop/126/like
    [HttpPost("{id:int}/like")]
    public IActionResult LikeCrop([FromRoute] int id)
    {
        _repo.IncrementLikes(id);
        return Ok();
    }
}

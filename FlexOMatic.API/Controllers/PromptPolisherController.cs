using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class PromptPolisherController : ControllerBase
{
    private readonly IPromptPolisher _polisher;

    public PromptPolisherController(IPromptPolisher polisher)
    {
        _polisher = polisher;
    }

    [HttpPost]
    public async Task<IActionResult> PolishPrompt([FromBody] PromptRequest request)
    {
        var polished = await _polisher.PolishAsync(request.UserPrompt);
        return Ok(new { polishedPrompt = polished });
    }
}

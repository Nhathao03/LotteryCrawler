using Microsoft.AspNetCore.Mvc;
using LotteryCrawler.Models;
using LotteryCrawler.Interface;

namespace LotteryCrawler.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClaudeController : ControllerBase
{
    private readonly IAIService _ai;

    public ClaudeController(IAIService ai)
    {
        _ai = ai;
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] ClaudeRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Prompt))
            return BadRequest("Prompt is required.");

        var result = await _ai.SendAsync(req.Prompt);
        return Ok(new { result });
    }
}

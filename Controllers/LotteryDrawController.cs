using Microsoft.AspNetCore.Mvc;
using LotteryCrawler.Interface;

namespace LotteryCrawler.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LotteryDrawController : ControllerBase
    {
        private readonly ILotteryDrawService _drawService;
        public LotteryDrawController(ILotteryDrawService drawService)
        {
            _drawService = drawService;
        }

        [HttpGet("crawl")]
        public async Task<IActionResult> Crawl([FromQuery] string url, [FromQuery] string elementId)
        {
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(elementId))
                return BadRequest("url and elementId are required");
            var rows = await _drawService.GetDrawRowsAsync(url, elementId);
            return Ok(rows);
        }
    }
}

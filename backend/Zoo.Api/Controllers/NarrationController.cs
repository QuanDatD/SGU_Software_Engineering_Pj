using Microsoft.AspNetCore.Mvc;
using Zoo.BUS.DTOs;
using Zoo.BUS.Interfaces;

namespace Zoo.Api.Controllers;

[ApiController]
[Route("api")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class NarrationController : ControllerBase
{
    private readonly INarrationService _narrationService;

    public NarrationController(INarrationService narrationService)
    {
        _narrationService = narrationService;
    }

    /// <summary>
    /// API cho Du khách: Lấy thông tin thuyết minh + Link Audio MP3 theo địa điểm và ngôn ngữ
    /// GET /api/places/{placeId}/narration?lang=en
    /// </summary>
    [HttpGet("places/{placeId:guid}/narration")]
    public async Task<ActionResult<NarrationDto>> GetNarration(
        Guid placeId,
        [FromQuery] string? lang = "vi",
        CancellationToken ct = default)
    {
        var narration = await _narrationService.GetNarrationAsync(placeId, lang ?? "vi", ct);
        if (narration == null)
        {
            return NotFound(new { message = "Không tìm thấy nội dung thuyết minh cho địa điểm này." });
        }
        return Ok(narration);
    }

}

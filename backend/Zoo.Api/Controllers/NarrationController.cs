using Microsoft.AspNetCore.Mvc;
using Zoo.BUS.DTOs;
using Zoo.BUS.Interfaces;

namespace Zoo.Api.Controllers;

[ApiController]
[Route("api")]
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

    /// <summary>
    /// API cho Quản trị viên: Kích hoạt dịch tự động và tạo file MP3 cho 1 địa điểm
    /// POST /api/admin/narrations/{placeId}/generate?force=false
    /// </summary>
    [HttpPost("admin/narrations/{placeId:guid}/generate")]
    public async Task<ActionResult<GenerationResultDto>> GenerateForPlace(
        Guid placeId,
        [FromQuery] bool force = false,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _narrationService.GenerateNarrationContentAsync(placeId, force, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Lỗi khi xử lý qua dịch vụ Azure: " + ex.Message });
        }
    }

    /// <summary>
    /// API cho Quản trị viên: Kích hoạt dịch và tạo audio cho TOÀN BỘ 7 khu cùng lúc
    /// POST /api/admin/narrations/generate-all?force=false
    /// </summary>
    [HttpPost("admin/narrations/generate-all")]
    public async Task<ActionResult<List<GenerationResultDto>>> GenerateAll(
        [FromQuery] bool force = false,
        CancellationToken ct = default)
    {
        var results = await _narrationService.GenerateAllPlacesAsync(force, ct);
        return Ok(results);
    }
}
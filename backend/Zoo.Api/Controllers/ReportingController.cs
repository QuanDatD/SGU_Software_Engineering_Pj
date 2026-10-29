using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zoo.BUS.DTOs;
using Zoo.BUS.Interfaces;

namespace Zoo.Api.Controllers;

[ApiController]
[Route("api/admin/reporting")]
[Authorize(Roles = "Admin")]
public sealed class ReportingController(IReportingService reportingService) : ControllerBase
{
    /// <summary>
    /// UC07.1–07.3: Trả tổng lượt, 24 khung giờ (VN) và giờ cao điểm.
    /// GET /api/admin/reporting?from=2026-01-01&amp;to=2026-12-31&amp;includeDemo=false
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ReportingResponse>> Get(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] bool includeDemo = false,
        CancellationToken cancellationToken = default)
    {
        if (from >= to)
            return BadRequest(new { message = "Ngày bắt đầu phải trước ngày kết thúc." });

        if ((to.DayNumber - from.DayNumber) > 366)
            return BadRequest(new { message = "Khoảng lọc tối đa 366 ngày." });

        var result = await reportingService.GetAsync(from, to, includeDemo, cancellationToken);
        return Ok(result);
    }
}

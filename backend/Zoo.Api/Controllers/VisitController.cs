using Microsoft.AspNetCore.Mvc;
using Zoo.BUS.DTOs;
using Zoo.BUS.Interfaces;

namespace Zoo.Api.Controllers;

[ApiController]
[Route("api/visit")]
public sealed class VisitController(IGeofenceService geofenceService, IQrVisitService qrVisitService)
    : ControllerBase
{
    // UC03.1 — Đánh giá vùng GPS
    [HttpPost("evaluate")]
    public async Task<ActionResult<EvaluateVisitResponse>> Evaluate(
        [FromBody] EvaluateVisitRequest request,
        CancellationToken cancellationToken)
    {
        var result = await geofenceService.EvaluateAsync(request, cancellationToken);
        return Ok(result);
    }

    // UC01.3 — Ghi nhận lượt truy cập qua QR (BR-20 – BR-26)
    [HttpPost("qr")]
    public async Task<ActionResult<RecordQrVisitResponse>> RecordQrVisit(
        [FromBody] RecordQrVisitRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId) || request.SessionId.Length > 200)
            return BadRequest(new { message = "SessionId không hợp lệ." });

        var result = await qrVisitService.RecordAsync(request, cancellationToken);
        return Ok(result);
    }
}

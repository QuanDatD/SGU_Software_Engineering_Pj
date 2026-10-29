using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zoo.BUS.DTOs;
using Zoo.BUS.Interfaces;

namespace Zoo.Api.Controllers;

[ApiController]
[Route("api/visit")]
public sealed class VisitController : ControllerBase
{
    private readonly IGeofenceService _geofenceService;

    public VisitController(IGeofenceService geofenceService)
    {
        _geofenceService = geofenceService;
    }

    // UC03.1 — Đánh giá vùng GPS
    [HttpPost("evaluate")]
    public async Task<ActionResult<EvaluateVisitResponse>> Evaluate(
        [FromBody] EvaluateVisitRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _geofenceService.EvaluateAsync(request, cancellationToken);
        return Ok(result);
    }

    // UC01.3 — Ghi nhận lượt truy cập qua QR (BR-20 – BR-26)
    [HttpPost("qr")]
    public async Task<ActionResult<RecordQrVisitResponse>> RecordQrVisit(
        [FromBody] RecordQrVisitRequest request,
        [FromServices] Zoo.DAL.Data.ZooDbContext db,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId) || request.SessionId.Length > 200)
            return BadRequest(new { message = "SessionId không hợp lệ." });

        // BR-21: mỗi SessionId chỉ ghi một lượt
        var exists = await db.QrVisits
            .AnyAsync(v => v.SessionId == request.SessionId, cancellationToken);

        if (exists)
            return Ok(new RecordQrVisitResponse(false, "Đã ghi nhận phiên này trước đó."));

        db.QrVisits.Add(new Zoo.DAL.Entities.QrVisit
        {
            SessionId = request.SessionId.Trim(),
            VisitedAtUtc = DateTime.UtcNow,  // BR-23: server ghi UTC
            IsDemo = request.IsDemo
        });

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new RecordQrVisitResponse(true, "Đã ghi nhận."));
    }
}

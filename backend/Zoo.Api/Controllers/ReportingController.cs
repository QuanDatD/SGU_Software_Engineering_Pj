using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zoo.BUS.DTOs;
using Zoo.DAL.Data;

namespace Zoo.Api.Controllers;

[ApiController]
[Route("api/admin/reporting")]
[Authorize(Roles = "Admin")]
public sealed class ReportingController(ZooDbContext db) : ControllerBase
{
    // Múi giờ Việt Nam để quy đổi giờ — BR-23
    private static readonly TimeZoneInfo VnTz =
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    /// <summary>
    /// UC07.1–07.3: Trả tổng lượt, 24 khung giờ (VN) và giờ cao điểm.
    /// GET /api/admin/reporting?from=2026-01-01&to=2026-12-31&includeDemo=false
    /// Khoảng lọc [from, to) theo ngày VN — BR-23.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ReportingResponse>> Get(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] bool includeDemo = false,
        CancellationToken cancellationToken = default)
    {
        // Validate khoảng thời gian
        if (from >= to)
            return BadRequest(new { message = "Ngày bắt đầu phải trước ngày kết thúc." });

        if ((to.DayNumber - from.DayNumber) > 366)
            return BadRequest(new { message = "Khoảng lọc tối đa 366 ngày." });

        // Quy đổi khoảng lọc từ ngày VN sang UTC [fromUtc, toUtc)
        var fromLocal = from.ToDateTime(TimeOnly.MinValue);
        var toLocal   = to.ToDateTime(TimeOnly.MinValue);
        var fromUtc = TimeZoneInfo.ConvertTimeToUtc(fromLocal, VnTz);
        var toUtc   = TimeZoneInfo.ConvertTimeToUtc(toLocal, VnTz);

        var query = db.QrVisits.AsNoTracking()
            .Where(v => v.VisitedAtUtc >= fromUtc && v.VisitedAtUtc < toUtc);

        // BR-26: loại dữ liệu demo khỏi báo cáo vận hành mặc định
        if (!includeDemo)
            query = query.Where(v => !v.IsDemo);

        var visits = await query
            .Select(v => v.VisitedAtUtc)
            .ToListAsync(cancellationToken);

        int total = visits.Count;

        // Nhóm theo 24 khung giờ VN — BR-24
        var buckets = new int[24];
        foreach (var utcTime in visits)
        {
            var vnTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, VnTz);
            buckets[vnTime.Hour]++;
        }

        var hourBuckets = Enumerable.Range(0, 24)
            .Select(h => new HourBucket(h, buckets[h]))
            .ToArray();

        // Xác định giờ cao điểm — BR-24: đồng hạng hiển thị đủ
        int[] peakHours = [];
        if (total > 0)
        {
            int maxCount = buckets.Max();
            peakHours = Enumerable.Range(0, 24)
                .Where(h => buckets[h] == maxCount)
                .ToArray();
        }

        return Ok(new ReportingResponse(total, hourBuckets, peakHours));
    }
}

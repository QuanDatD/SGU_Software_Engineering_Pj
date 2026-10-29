using Microsoft.EntityFrameworkCore;
using Zoo.BUS.DTOs;
using Zoo.BUS.Interfaces;
using Zoo.DAL.Data;

namespace Zoo.BUS.Services;

public sealed class ReportingService(ZooDbContext db) : IReportingService
{
    // Múi giờ Việt Nam — BR-23
    private static readonly TimeZoneInfo VnTz =
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    public async Task<ReportingResponse> GetAsync(
        DateOnly from,
        DateOnly to,
        bool includeDemo,
        CancellationToken ct = default)
    {
        // Quy đổi khoảng lọc từ ngày VN sang UTC [fromUtc, toUtc)
        var fromUtc = TimeZoneInfo.ConvertTimeToUtc(from.ToDateTime(TimeOnly.MinValue), VnTz);
        var toUtc   = TimeZoneInfo.ConvertTimeToUtc(to.ToDateTime(TimeOnly.MinValue),   VnTz);

        var query = db.QrVisits.AsNoTracking()
            .Where(v => v.VisitedAtUtc >= fromUtc && v.VisitedAtUtc < toUtc);

        // BR-26: loại dữ liệu demo khỏi báo cáo vận hành mặc định
        if (!includeDemo)
            query = query.Where(v => !v.IsDemo);

        var visits = await query.Select(v => v.VisitedAtUtc).ToListAsync(ct);

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

        // Giờ cao điểm — BR-24: đồng hạng hiển thị đủ
        int[] peakHours = [];
        if (total > 0)
        {
            int maxCount = buckets.Max();
            peakHours = Enumerable.Range(0, 24)
                .Where(h => buckets[h] == maxCount)
                .ToArray();
        }

        return new ReportingResponse(total, hourBuckets, peakHours);
    }
}

using Microsoft.EntityFrameworkCore;
using Zoo.BUS.DTOs;
using Zoo.BUS.Interfaces;
using Zoo.DAL.Data;
using Zoo.DAL.Entities;

namespace Zoo.BUS.Services;

public sealed class QrVisitService(ZooDbContext db) : IQrVisitService
{
    private const int RetentionDays = 90;

    public async Task<RecordQrVisitResponse> RecordAsync(
        RecordQrVisitRequest request,
        CancellationToken ct = default)
    {
        var sessionId = request.SessionId.Trim();
        var nowUtc = DateTime.UtcNow;

        // Dọn các lượt QR quá 90 ngày
        var cutoffUtc = nowUtc.AddDays(-RetentionDays);
        await db.QrVisits
            .Where(v => v.VisitedAtUtc < cutoffUtc)
            .ExecuteDeleteAsync(ct);

        // BR-21: mỗi SessionId chỉ ghi một lượt
        var exists = await db.QrVisits
            .AsNoTracking()
            .AnyAsync(v => v.SessionId == sessionId, ct);

        if (exists)
            return new RecordQrVisitResponse(false, "Đã ghi nhận phiên này trước đó.");

        try
        {
            db.QrVisits.Add(new QrVisit
            {
                SessionId = sessionId,
                VisitedAtUtc = nowUtc,
                IsDemo = request.IsDemo
            });
            await db.SaveChangesAsync(ct);
            return new RecordQrVisitResponse(true, "Đã ghi nhận.");
        }
        catch (DbUpdateException ex)
            when (ex.InnerException?.Message.Contains("IX_QrVisits_SessionId",
                      StringComparison.OrdinalIgnoreCase) == true
               || ex.InnerException?.Message.Contains("UNIQUE",
                      StringComparison.OrdinalIgnoreCase) == true)
        {
            return new RecordQrVisitResponse(false, "Đã ghi nhận phiên này trước đó.");
        }
    }
}
using Microsoft.EntityFrameworkCore;
using Zoo.BUS.DTOs;
using Zoo.BUS.Interfaces;
using Zoo.DAL.Data;
using Zoo.DAL.Entities;

namespace Zoo.BUS.Services;

public sealed class QrVisitService(ZooDbContext db) : IQrVisitService
{
    public async Task<RecordQrVisitResponse> RecordAsync(
        RecordQrVisitRequest request,
        CancellationToken ct = default)
    {
        // BR-21: mỗi SessionId chỉ ghi một lượt
        var exists = await db.QrVisits
            .AsNoTracking()
            .AnyAsync(v => v.SessionId == request.SessionId, ct);

        if (exists)
            return new RecordQrVisitResponse(false, "Đã ghi nhận phiên này trước đó.");

        try
        {
            db.QrVisits.Add(new QrVisit
            {
                SessionId = request.SessionId.Trim(),
                VisitedAtUtc = DateTime.UtcNow, // BR-23: server ghi UTC
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
            // BR-21: race condition — hai request cùng SessionId đến đồng thời
            // Unique index bắt được → bỏ qua, không báo lỗi
            return new RecordQrVisitResponse(false, "Đã ghi nhận phiên này trước đó.");
        }
    }
}

using Zoo.BUS.DTOs;

namespace Zoo.BUS.Interfaces;

public interface IQrVisitService
{
    /// <summary>
    /// Ghi nhận lượt truy cập qua QR. Mỗi SessionId chỉ ghi một lần (BR-21).
    /// Xử lý concurrent: nếu trùng SessionId do race condition → bỏ qua, không báo lỗi.
    /// </summary>
    Task<RecordQrVisitResponse> RecordAsync(RecordQrVisitRequest request, CancellationToken ct = default);
}

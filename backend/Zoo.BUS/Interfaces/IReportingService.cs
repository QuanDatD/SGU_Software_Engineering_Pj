using Zoo.BUS.DTOs;

namespace Zoo.BUS.Interfaces;

public interface IReportingService
{
    /// <summary>
    /// UC07.1–07.3: Lấy tổng lượt, 24 khung giờ VN và giờ cao điểm.
    /// Khoảng lọc [from, to) theo ngày VN — BR-23.
    /// </summary>
    Task<ReportingResponse> GetAsync(
        DateOnly from,
        DateOnly to,
        bool includeDemo,
        CancellationToken ct = default);
}

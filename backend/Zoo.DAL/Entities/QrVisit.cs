namespace Zoo.DAL.Entities;

/// <summary>
/// Lưu mỗi lượt mở website từ liên kết QR (BR-20 – BR-26).
/// Unique(SessionId) để chống trùng lượt.
/// </summary>
public class QrVisit
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Mã phiên trình duyệt — dùng để chống ghi trùng lượt.</summary>
    public string SessionId { get; set; } = null!;

    /// <summary>Thời điểm ghi nhận (UTC) do server tạo, không dùng đồng hồ client.</summary>
    public DateTime VisitedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Đánh dấu lượt do demo/kiểm thử — loại khỏi báo cáo vận hành mặc định.</summary>
    public bool IsDemo { get; set; } = false;
}

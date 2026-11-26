namespace Zoo.BUS.DTOs;

/// <summary>
/// Yêu cầu xuất bản phiên bản narration với xác nhận điều kiện
/// </summary>
public class PublishRevisionRequest
{
    /// <summary>
    /// Người dùng xác nhận đã kiểm tra tất cả 5 ngôn ngữ
    /// </summary>
    public bool ConfirmAllLanguagesReady { get; set; }

    /// <summary>
    /// Người dùng xác nhận tất cả audio hợp lệ
    /// </summary>
    public bool ConfirmAllAudioValid { get; set; }

    /// <summary>
    /// Người dùng xác nhận tất cả job đã xong
    /// </summary>
    public bool ConfirmAllJobsCompleted { get; set; }

    /// <summary>
    /// Người dùng xác nhận tất cả bản dịch đã duyệt (không có NeedsReview=true)
    /// </summary>
    public bool ConfirmAllTranslationsReviewed { get; set; }
}

/// <summary>
/// Kết quả xuất bản
/// </summary>
public class PublishRevisionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    
    /// <summary>
    /// Danh sách điều kiện chưa đủ (nếu Success = false)
    /// </summary>
    public List<string> MissingConditions { get; set; } = new();
}

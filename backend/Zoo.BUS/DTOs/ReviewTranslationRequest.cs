namespace Zoo.BUS.DTOs;

/// <summary>
/// Yêu cầu duyệt lại bản dịch khi script tiếng Việt thay đổi
/// </summary>
public class ReviewTranslationRequest
{
    /// <summary>
    /// Hành động duyệt: "KeepOld" = giữ bản dịch cũ, "UpdateToNew" = cập nhật sang script Vi mới
    /// </summary>
    public string Action { get; set; } = "KeepOld"; // "KeepOld" | "UpdateToNew"
}

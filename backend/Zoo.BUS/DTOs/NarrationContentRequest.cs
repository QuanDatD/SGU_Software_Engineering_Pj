namespace Zoo.BUS.DTOs;

/// <summary>
/// Yêu cầu cập nhật content tiếng Việt của phiên bản narration
/// </summary>
public class NarrationContentRequest
{
    /// <summary>
    /// Tiêu đề tiếng Việt
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Mô tả tiếng Việt
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Kịch bản đọc (script) tiếng Việt
    /// </summary>
    public string? ScriptText { get; set; }
}

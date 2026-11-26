namespace Zoo.BUS.DTOs;

/// <summary>
/// Thông tin tiến độ job tạo âm thanh (UC06 narration generation)
/// </summary>
public class JobProgressDto
{
    public Guid Id { get; set; }
    public Guid RevisionId { get; set; }
    public string Language { get; set; } = "";
    
    // Status & Stage
    public string Status { get; set; } = "queued"; // queued, processing, succeeded, failed
    public int AttemptCount { get; set; }
    public string Stage { get; set; } = "translate"; // translate, synthesize, store
    
    // Error & Recovery
    public string? ErrorMessage { get; set; }
    public bool CanRetry { get; set; } // true nếu status = failed hoặc (gián đoạn)
    
    // Audio preview (khi ready) - Thao tác nghe thử
    public DateTime? CreatedAt { get; set; }
    public bool CanListen { get; set; } // true nếu audio ready và hợp lệ
    
    // Translation review status
    public DateTime? UpdatedAt { get; set; }
    public bool NeedsReview { get; set; }
}

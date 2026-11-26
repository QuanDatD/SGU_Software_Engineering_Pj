using Zoo.DAL.Enums;

namespace Zoo.DAL.Entities;

public class NarrationTranslation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RevisionId { get; set; }
    public string Language { get; set; } = null!;      // vi | en | zh | ko | ja
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ScriptText { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string? SourceContentHash { get; set; }     // Hash nguồn tiếng Việt mà bản dịch dựa vào
    public bool NeedsReview { get; set; } = false;     // Trạng thái "Cần duyệt lại"
    public string? AudioPath { get; set; }
    public string? AudioContentHash { get; set; }
    public AudioStatus AudioStatus { get; set; } = AudioStatus.Missing;
    public bool IsEditedByAdmin { get; set; } = false;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public NarrationRevision Revision { get; set; } = null!;
}
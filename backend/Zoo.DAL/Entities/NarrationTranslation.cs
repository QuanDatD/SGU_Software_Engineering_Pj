using Zoo.DAL.Enums;

namespace Zoo.DAL.Entities;

public class NarrationTranslation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RevisionId { get; set; }
    public string Language { get; set; } = null!;      // vi | en | zh | ko | ja
    public string Title { get; set; } = string.Empty;  // Tên khu theo ngôn ngữ
    public string Description { get; set; } = string.Empty;
    public string ScriptText { get; set; } = string.Empty; // Bài thuyết minh
    public string ContentHash { get; set; } = string.Empty;
    public string? AudioPath { get; set; }             // Đường dẫn tương đối trong kho storage
    public string? AudioContentHash { get; set; }
    public AudioStatus AudioStatus { get; set; } = AudioStatus.Missing;
    public bool IsEditedByAdmin { get; set; } = false; // Chống ghi đè khi retry
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public NarrationRevision Revision { get; set; } = null!;
}
using Zoo.DAL.Enums;

namespace Zoo.DAL.Entities;

public class GenerationJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RevisionId { get; set; }
    public string Language { get; set; } = null!;      // vi | en | zh | ko | ja
    public JobStage Stage { get; set; } = JobStage.Translate;
    public JobStatus Status { get; set; } = JobStatus.Queued;
    public int AttemptCount { get; set; } = 0;
    public DateTime? LockedUntilUtc { get; set; }      // Thời hạn giữ job
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public NarrationRevision Revision { get; set; } = null!;
}
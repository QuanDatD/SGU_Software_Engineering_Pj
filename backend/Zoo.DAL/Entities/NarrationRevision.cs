using Zoo.DAL.Enums;

namespace Zoo.DAL.Entities;

public class NarrationRevision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlaceId { get; set; }
    public int Version { get; set; }                   // (PlaceId, Version) không trùng
    public RevisionStatus Status { get; set; } = RevisionStatus.Draft;
    public Guid? CreatedByAdminId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }

    public Place Place { get; set; } = null!;
    public ICollection<NarrationTranslation> Translations { get; set; } = new List<NarrationTranslation>();
    public ICollection<GenerationJob> Jobs { get; set; } = new List<GenerationJob>();
}
using Zoo.DAL.Entities;

namespace Zoo.DAL.Interfaces;

public interface INarrationRepository
{
    Task<NarrationRevision?> GetRevisionAsync(Guid revisionId, CancellationToken ct = default);
    Task<int> GetLatestVersionAsync(Guid placeId, CancellationToken ct = default);
    Task AddRevisionAsync(NarrationRevision revision, CancellationToken ct = default);
    Task<NarrationTranslation?> GetTranslationByIdAsync(Guid translationId, CancellationToken ct = default);
    Task<NarrationTranslation?> GetTranslationAsync(Guid revisionId, string language, CancellationToken ct = default);
    Task AddTranslationAsync(NarrationTranslation translation, CancellationToken ct = default);
}
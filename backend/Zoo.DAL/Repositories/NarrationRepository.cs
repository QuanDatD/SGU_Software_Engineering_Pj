using Microsoft.EntityFrameworkCore;
using Zoo.DAL.Data;
using Zoo.DAL.Entities;
using Zoo.DAL.Interfaces;

namespace Zoo.DAL.Repositories;

public class NarrationRepository : INarrationRepository
{
    private readonly ZooDbContext _db;
    public NarrationRepository(ZooDbContext db) => _db = db;

    public Task<NarrationRevision?> GetRevisionAsync(Guid revisionId, CancellationToken ct = default)
        => _db.NarrationRevisions
            .Include(r => r.Translations)
            .Include(r => r.Jobs)
            .Include(r => r.Place)
            .FirstOrDefaultAsync(r => r.Id == revisionId, ct);

    public async Task<int> GetLatestVersionAsync(Guid placeId, CancellationToken ct = default)
        => await _db.NarrationRevisions
            .Where(r => r.PlaceId == placeId)
            .Select(r => (int?)r.Version)
            .MaxAsync(ct) ?? 0;

    public async Task AddRevisionAsync(NarrationRevision revision, CancellationToken ct = default)
        => await _db.NarrationRevisions.AddAsync(revision, ct);

    public Task<NarrationTranslation?> GetTranslationByIdAsync(Guid translationId, CancellationToken ct = default)
        => _db.NarrationTranslations
            .Include(t => t.Revision)
            .FirstOrDefaultAsync(t => t.Id == translationId, ct);

    public Task<NarrationTranslation?> GetTranslationAsync(Guid revisionId, string language, CancellationToken ct = default)
        => _db.NarrationTranslations
            .FirstOrDefaultAsync(t => t.RevisionId == revisionId && t.Language == language, ct);

    public async Task AddTranslationAsync(NarrationTranslation translation, CancellationToken ct = default)
        => await _db.NarrationTranslations.AddAsync(translation, ct);
}
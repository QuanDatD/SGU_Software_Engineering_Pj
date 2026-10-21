using Microsoft.EntityFrameworkCore;
using Zoo.DAL.Data;
using Zoo.DAL.Entities;
using Zoo.DAL.Interfaces;

namespace Zoo.DAL.Repositories;

public class SiteTextRepository : ISiteTextRepository
{
    private readonly ZooDbContext _db;
    public SiteTextRepository(ZooDbContext db) => _db = db;

    public Task<List<SiteText>> GetByLanguageAsync(string language, CancellationToken ct = default)
        => _db.SiteTexts.Where(x => x.Language == language).ToListAsync(ct);

    public async Task UpsertAsync(string key, string language, string value, CancellationToken ct = default)
    {
        var existing = await _db.SiteTexts.FirstOrDefaultAsync(x => x.Key == key && x.Language == language, ct);
        if (existing == null)
        {
            await _db.SiteTexts.AddAsync(new SiteText
            {
                Key = key,
                Language = language,
                Value = value,
                UpdatedAt = DateTime.UtcNow
            }, ct);
        }
        else
        {
            existing.Value = value;
            existing.UpdatedAt = DateTime.UtcNow;
        }
    }
}
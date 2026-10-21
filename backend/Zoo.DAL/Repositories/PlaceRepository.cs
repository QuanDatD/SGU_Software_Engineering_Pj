using Microsoft.EntityFrameworkCore;
using Zoo.DAL.Data;
using Zoo.DAL.Entities;
using Zoo.DAL.Interfaces;

namespace Zoo.DAL.Repositories;

public class PlaceRepository : IPlaceRepository
{
    private readonly ZooDbContext _db;
    public PlaceRepository(ZooDbContext db) => _db = db;

    public Task<Place?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Places
            .Include(p => p.PublishedRevision)
                .ThenInclude(r => r!.Translations)
            .Include(p => p.Revisions)
                .ThenInclude(r => r.Translations)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Place?> GetByCodeAsync(string code, CancellationToken ct = default)
        => _db.Places.FirstOrDefaultAsync(p => p.Code == code, ct);

    public Task<List<Place>> GetPublishedAsync(CancellationToken ct = default)
        => _db.Places
            .Include(p => p.PublishedRevision)
                .ThenInclude(r => r!.Translations)
            .Where(p => p.IsActive && p.PublishedRevisionId != null)
            .OrderByDescending(p => p.Priority)
            .ToListAsync(ct);

    public Task<List<Place>> GetAllAsync(CancellationToken ct = default)
        => _db.Places
            .Include(p => p.PublishedRevision)
            .Include(p => p.Revisions)
            .OrderByDescending(p => p.Priority)
            .ToListAsync(ct);

    public async Task AddAsync(Place place, CancellationToken ct = default)
        => await _db.Places.AddAsync(place, ct);

    public void Update(Place place)
    {
        place.UpdatedAt = DateTime.UtcNow;
        _db.Places.Update(place);
    }
}
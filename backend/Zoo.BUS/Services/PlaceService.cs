using Microsoft.EntityFrameworkCore;
using Zoo.BUS.DTOs;
using Zoo.BUS.Interfaces;
using Zoo.DAL.Data;
using Zoo.DAL.Entities;
using Zoo.DAL.Enums;
namespace Zoo.BUS.Services;

public sealed class PlaceService(ZooDbContext db) : IPlaceService, IAdminPlaceService
{
    private IQueryable<Place> PublicQuery() => db.Places.AsNoTracking()
        .Include(p => p.PublishedRevision).ThenInclude(r => r!.Translations)
        .Where(p => p.IsActive && p.PublishedRevision != null
            && p.PublishedRevision.PlaceId == p.Id && p.PublishedRevision.Status == RevisionStatus.Published);
    public async Task<IReadOnlyList<PlaceDto>> GetPublishedAsync(string language, CancellationToken cancellationToken = default)
        => (await PublicQuery().OrderByDescending(p => p.Priority).ToListAsync(cancellationToken))
            .Select(p => Map(p, p.PublishedRevision)).ToList();
    public async Task<PlaceDto?> GetPublishedByIdAsync(Guid id, string language, CancellationToken cancellationToken = default)
    {
        var p = await PublicQuery().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        return p == null ? null : Map(p, p.PublishedRevision);
    }
    public Task<IReadOnlyList<PlaceDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => GetAllForAdminAsync(cancellationToken);
    public async Task<IReadOnlyList<PlaceDto>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
        => (await db.Places.AsNoTracking().Include(p => p.Revisions).ThenInclude(r => r.Translations)
            .OrderByDescending(p => p.CreatedAt).ToListAsync(cancellationToken))
            .Select(p => Map(p, p.Revisions.OrderByDescending(r => r.Version).FirstOrDefault())).ToList();
    private static PlaceDto Map(Place p, NarrationRevision? revision) => new()
    {
        Id = p.Id, Code = p.Code, Slug = p.Code.ToLowerInvariant(), Category = p.Category,
        Icon = p.Category == "animal" ? "🐾" : p.Category == "plant" ? "🌳" : "📍",
        Lat = p.Latitude, Lng = p.Longitude, RadiusMeters = p.RadiusMeters,
        MapX = p.MapX, MapY = p.MapY, Priority = p.Priority, IsActive = p.IsActive,
        ImageUrl = p.ImageUrl, RevisionId = revision?.Id, Version = revision?.Version,
        Name = revision?.Translations.ToDictionary(t => t.Language, t => t.Title) ?? new(),
        Desc = revision?.Translations.ToDictionary(t => t.Language, t => t.Description) ?? new(),
        Script = revision?.Translations.ToDictionary(t => t.Language, t => t.ScriptText) ?? new(),
        AudioUrl = revision?.Translations.ToDictionary(t => t.Language, t =>
            PublicationRules.Ready(t) && revision.Id == p.PublishedRevisionId && revision.Status == RevisionStatus.Published
            ? $"/api/places/{p.Id}/audio/{t.Language}?revisionId={revision.Id}&hash={t.AudioContentHash}" : (string?)null) ?? new()
    };
}

using Microsoft.EntityFrameworkCore;
using Zoo.BUS.DTOs;
using Zoo.BUS.Interfaces;
using Zoo.DAL.Data;
using Zoo.DAL.Entities;

namespace Zoo.BUS.Services;

public sealed class PlaceService : IPlaceService
{
    private readonly ZooDbContext _db;

    public PlaceService(ZooDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PlaceDto>> GetPublishedAsync(
        string language,
        CancellationToken cancellationToken = default)
    {
        // Lấy tất cả các khu Active và đã có Revision xuất bản
        var places = await _db.Places
            .AsNoTracking()
            .Include(p => p.PublishedRevision)
                .ThenInclude(r => r!.Translations)
            .Where(p => p.IsActive && p.PublishedRevisionId != null)
            .OrderByDescending(p => p.Priority)
            .ToListAsync(cancellationToken);

        return places.Select(MapToDto).ToList();
    }

    public async Task<PlaceDto?> GetPublishedByIdAsync(
        Guid id,
        string language,
        CancellationToken cancellationToken = default)
    {
        var place = await _db.Places
            .AsNoTracking()
            .Include(p => p.PublishedRevision)
                .ThenInclude(r => r!.Translations)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive && p.PublishedRevisionId != null, cancellationToken);

        return place is null ? null : MapToDto(place);
    }

    private static PlaceDto MapToDto(Place p)
    {
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var descs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var audios = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        var langs = new[] { "vi", "en", "zh", "ko", "ja" };
        foreach (var l in langs)
        {
            names[l] = "";
            descs[l] = "";
            audios[l] = null;
        }

        if (p.PublishedRevision != null)
        {
            foreach (var t in p.PublishedRevision.Translations)
            {
                var l = t.Language.ToLowerInvariant();
                names[l] = t.Title;
                descs[l] = string.IsNullOrWhiteSpace(t.ScriptText) ? t.Description : t.ScriptText;

                if (!string.IsNullOrEmpty(t.AudioPath))
                {
                    audios[l] = $"/storage/{t.AudioPath.TrimStart('/')}";
                }
            }
        }

        return new PlaceDto
        {
            Id = p.Id,
            Slug = p.Code.ToLowerInvariant().Replace('_', '-'),
            Code = p.Code,
            Icon = GetIconForCategory(p.Code, p.Category),
            Category = p.Category.ToLowerInvariant(),
            Lat = p.Latitude,
            Lng = p.Longitude,
            RadiusMeters = p.RadiusMeters,
            MapX = p.MapX,
            MapY = p.MapY,
            Priority = p.Priority,
            IsActive = p.IsActive,
            ImageUrl = p.ImageUrl ?? $"/images/{p.Code.ToLowerInvariant().Replace('_', '-')}.jpg",
            Name = names,
            Desc = descs,
            AudioUrl = audios
        };
    }

    private static string GetIconForCategory(string code, string category)
    {
        return code.ToUpperInvariant() switch
        {
            var c when c.Contains("ELEPHANT") => "🐘",
            var c when c.Contains("TIGER") => "🐯",
            var c when c.Contains("GIRAFFE") => "🦒",
            var c when c.Contains("BIRD") || c.Contains("REPTILE") => "🐊",
            var c when c.Contains("MUSEUM") || c.Contains("ENTRANCE") => "🏛️",
            var c when c.Contains("TREE") || c.Contains("BOTANICAL") => "🌳",
            _ => category.ToLowerInvariant() switch
            {
                "animal" => "🐾",
                "plant" => "🌿",
                _ => "📍"
            }
        };
    }
}
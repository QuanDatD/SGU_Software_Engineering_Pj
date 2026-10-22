using Microsoft.EntityFrameworkCore;
using Zoo.BUS.DTOs;
using Zoo.BUS.Interfaces;
using Zoo.DAL.Data;
using Zoo.DAL.Entities;
using Zoo.DAL.Enums;

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
        var places = await _db.Places
            .AsNoTracking()
            .Include(p => p.PublishedRevision)
                .ThenInclude(r => r!.Translations)
            .Include(p => p.Revisions)
                .ThenInclude(r => r.Translations)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.Priority)
            .ToListAsync(cancellationToken);

        return places
            .Select(MapToDto)
            .Where(dto => dto.Name.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
            .ToList();
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
            .Include(p => p.Revisions)
                .ThenInclude(r => r.Translations)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken);

        return place is null ? null : MapToDto(place);
    }

    public async Task<IReadOnlyList<PlaceDto>> GetAllForAdminAsync(
        CancellationToken cancellationToken = default)
    {
        var places = await _db.Places
            .AsNoTracking()
            .Include(p => p.PublishedRevision)
                .ThenInclude(r => r!.Translations)
            .Include(p => p.Revisions)
                .ThenInclude(r => r.Translations)
            .OrderByDescending(p => p.Priority)
            .ToListAsync(cancellationToken);

        return places.Select(MapToDto).ToList();
    }

    private static PlaceDto MapToDto(Place p)
    {
        var langs = new[] { "vi", "en", "zh", "ko", "ja" };
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var descs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var audios = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var l in langs)
        {
            names[l] = GetDefaultTitle(p.Code);
            descs[l] = "";
            audios[l] = null;
        }

        // Ưu tiên lấy PublishedRevision; nếu chưa gán khóa ngoại thì lấy Revision Published mới nhất
        var activeRevision = p.PublishedRevision
            ?? p.Revisions
                .Where(r => r.Status == RevisionStatus.Published)
                .OrderByDescending(r => r.Version)
                .FirstOrDefault()
            ?? p.Revisions
                .OrderByDescending(r => r.Version)
                .FirstOrDefault();

        if (activeRevision != null)
        {
            foreach (var t in activeRevision.Translations)
            {
                var l = t.Language.ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(t.Title) && !t.Title.Equals(p.Code, StringComparison.OrdinalIgnoreCase))
                {
                    names[l] = t.Title;
                }
                descs[l] = !string.IsNullOrWhiteSpace(t.ScriptText) ? t.ScriptText : t.Description;

                if (!string.IsNullOrWhiteSpace(t.AudioPath))
                {
                    audios[l] = $"/storage/{t.AudioPath.TrimStart('/')}";
                }
            }
        }

        // Nếu ngôn ngữ phụ chưa có tên, dùng tên tiếng Việt làm fallback
        foreach (var l in langs)
        {
            if (string.IsNullOrWhiteSpace(names[l]))
                names[l] = names["vi"];
        }

        var normalizedCat = p.Category.ToLowerInvariant() switch
        {
            "animal" => "animal",
            "plant" => "plant",
            _ => "facility"
        };

        return new PlaceDto
        {
            Id = p.Id,
            Slug = p.Code.ToLowerInvariant().Replace('_', '-'),
            Code = p.Code,
            Icon = GetIconForCategory(p.Code, normalizedCat),
            Category = normalizedCat,
            Lat = p.Latitude,
            Lng = p.Longitude,
            RadiusMeters = p.RadiusMeters,
            MapX = p.MapX <= 1.0 ? 500 : p.MapX,
            MapY = p.MapY <= 1.0 ? 300 : p.MapY,
            Priority = p.Priority,
            IsActive = p.IsActive,
            ImageUrl = p.ImageUrl ?? GetDefaultImage(p.Code),
            Name = names,
            Desc = descs,
            AudioUrl = audios
        };
    }

    private static string GetDefaultTitle(string code) => code.ToUpperInvariant() switch
    {
        "ENTRANCE_GATE" => "Cổng chính & Đền Hùng",
        "ASIAN_ELEPHANT" => "Khu Voi Châu Á",
        "GIRAFFE_ZONE" => "Khu Hươu Cao Cổ",
        "INDOCHINESE_TIGER" => "Khu Hổ Đông Dương",
        "BIRDS_REPTILES" => "Vườn Chim & Bò Sát",
        "ANCIENT_BOTANICAL" => "Vườn Bách Thảo Cổ Thụ",
        "HISTORY_MUSEUM" => "Bảo tàng Lịch sử TP.HCM",
        _ => code
    };

    private static string GetDefaultImage(string code) => code.ToUpperInvariant() switch
    {
        "ENTRANCE_GATE" => "/images/entrance.jpg",
        "ASIAN_ELEPHANT" => "/images/elephant.jpg",
        "GIRAFFE_ZONE" => "/images/giraffe.jpg",
        "INDOCHINESE_TIGER" => "/images/gallery-2.jpg",
        "BIRDS_REPTILES" => "/images/gallery-3.jpg",
        "ANCIENT_BOTANICAL" => "/images/ancient-tree.jpg",
        "HISTORY_MUSEUM" => "/images/gallery-1.jpg",
        _ => "/images/hero-bg.jpg"
    };

    private static string GetIconForCategory(string code, string category) => code.ToUpperInvariant() switch
    {
        var c when c.Contains("ELEPHANT") => "🐘",
        var c when c.Contains("TIGER") => "🐯",
        var c when c.Contains("GIRAFFE") => "🦒",
        var c when c.Contains("BIRD") || c.Contains("REPTILE") => "🐊",
        var c when c.Contains("MUSEUM") => "🏛️",
        var c when c.Contains("ENTRANCE") => "🚪",
        var c when c.Contains("TREE") || c.Contains("BOTANICAL") => "🌳",
        _ => category switch
        {
            "animal" => "🐾",
            "plant" => "🌿",
            _ => "📍"
        }
    };
}
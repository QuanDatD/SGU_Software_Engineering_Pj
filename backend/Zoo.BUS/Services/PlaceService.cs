using Zoo.BUS.DTOs;
using Zoo.BUS.Interfaces;
using Zoo.DAL.Entities;
using Zoo.DAL.Enums;
using Zoo.DAL.Interfaces;

namespace Zoo.BUS.Services;

public sealed class PlaceService :
    IPlaceService,
    IAdminPlaceService
{
    private readonly IPlaceRepository _repository;

    public PlaceService(IPlaceRepository repository)
    {
        _repository = repository;
    }

    // Khách chỉ thấy khu đang hoạt động và đã xuất bản.
    public async Task<IReadOnlyList<PlaceDto>> GetPublishedAsync(
        string language,
        CancellationToken cancellationToken = default)
    {
        var places = await _repository.GetPublishedAsync(
            cancellationToken);

        return places
            .Where(IsPublished)
            .Select(p => Map(
                p,
                p.PublishedRevision,
                language))
            .ToList();
    }

    public async Task<PlaceDto?> GetPublishedByIdAsync(
        Guid id,
        string language,
        CancellationToken cancellationToken = default)
    {
        var place = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (place is null || !IsPublished(place))
        {
            return null;
        }

        return Map(
            place,
            place.PublishedRevision,
            language);
    }

    // Admin thấy tất cả khu.
    public async Task<IReadOnlyList<PlaceDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var places = await _repository.GetAllAsync(
            cancellationToken);

        return places.Select(place =>
        {
            // Admin xem nội dung phiên bản mới nhất,
            // kể cả khi phiên bản đó còn là bản nháp.
            var revision = place.Revisions
                .OrderByDescending(r => r.Version)
                .FirstOrDefault();

            return Map(place, revision, "vi");
        }).ToList();
    }

    private static bool IsPublished(Place place)
    {
        var revision = place.PublishedRevision;

        return place.IsActive
            && revision is not null
            && place.PublishedRevisionId == revision.Id
            && revision.PlaceId == place.Id
            && revision.Status == RevisionStatus.Published;
    }

    private static PlaceDto Map(
        Place place,
        NarrationRevision? revision,
        string language)
    {
        var translations = revision?.Translations
            .ToList() ?? new List<NarrationTranslation>();

        var names = translations.ToDictionary(
            t => t.Language,
            t => t.Title);

        var descriptions = translations.ToDictionary(
            t => t.Language,
            t => t.Description);

        // Nếu chưa nhập tên tiếng Việt, admin vẫn thấy mã khu.
        if (!names.TryGetValue("vi", out var viName)
            || string.IsNullOrWhiteSpace(viName))
        {
            names["vi"] = place.Code;
        }

        var selected = translations
            .FirstOrDefault(t => t.Language == language);

        return new PlaceDto
        {
            Id = place.Id,
            Code = place.Code,
            Category = place.Category,
            Language = language,

            Name = selected?.Title ?? place.Code,
            Summary = selected?.Description ?? "",
            Script = selected?.ScriptText ?? "",

            Latitude = place.Latitude,
            Longitude = place.Longitude,
            RadiusMeters = place.RadiusMeters,
            Priority = place.Priority,

            MapX = place.MapX,
            MapY = place.MapY,
            IsActive = place.IsActive,

            ImageUrl = place.ImageUrl,

            // Sẽ nối URL khi có API phục vụ âm thanh.
            AudioUrl = null,

            Names = names,
            Descriptions = descriptions
        };
    }
}
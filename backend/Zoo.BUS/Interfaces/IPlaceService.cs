using Zoo.BUS.DTOs;

namespace Zoo.BUS.Interfaces;

public interface IPlaceService
{
    Task<IReadOnlyList<PlaceDto>> GetPublishedAsync(
        string language,
        CancellationToken cancellationToken = default);

    Task<PlaceDto?> GetPublishedByIdAsync(
        Guid id,
        string language,
        CancellationToken cancellationToken = default);
}
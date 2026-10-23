using Zoo.BUS.DTOs;

namespace Zoo.BUS.Interfaces;

public interface IAdminPlaceService
{
    Task<IReadOnlyList<PlaceDto>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
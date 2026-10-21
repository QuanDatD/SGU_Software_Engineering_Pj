using Zoo.DAL.Entities;

namespace Zoo.DAL.Interfaces;

public interface IPlaceRepository
{
    Task<Place?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Place?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<List<Place>> GetPublishedAsync(CancellationToken ct = default);
    Task<List<Place>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Place place, CancellationToken ct = default);
    void Update(Place place);
}
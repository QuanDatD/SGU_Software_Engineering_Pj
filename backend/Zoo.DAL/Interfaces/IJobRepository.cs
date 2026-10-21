using Zoo.DAL.Entities;

namespace Zoo.DAL.Interfaces;

public interface IJobRepository
{
    Task<GenerationJob?> TryClaimNextAsync(TimeSpan leaseDuration, CancellationToken ct = default);
    Task<List<GenerationJob>> GetByRevisionAsync(Guid revisionId, CancellationToken ct = default);
    Task<GenerationJob?> GetByIdAsync(Guid jobId, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<GenerationJob> jobs, CancellationToken ct = default);
    Task<bool> HasActiveJobsAsync(Guid revisionId, CancellationToken ct = default);
}
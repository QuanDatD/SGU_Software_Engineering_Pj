using Microsoft.EntityFrameworkCore;
using Zoo.DAL.Data;
using Zoo.DAL.Entities;
using Zoo.DAL.Enums;
using Zoo.DAL.Interfaces;

namespace Zoo.DAL.Repositories;

public class JobRepository : IJobRepository
{
    private readonly ZooDbContext _db;
    public JobRepository(ZooDbContext db) => _db = db;

    public async Task<GenerationJob?> TryClaimNextAsync(TimeSpan leaseDuration, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var job = await _db.GenerationJobs
            .Where(j => j.Status == JobStatus.Queued ||
                       (j.Status == JobStatus.Processing && j.LockedUntilUtc < now))
            .OrderBy(j => j.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (job == null)
            return null;

        job.Status = JobStatus.Processing;
        job.AttemptCount += 1;
        job.LockedUntilUtc = now.Add(leaseDuration);
        job.UpdatedAt = now;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return job;
    }

    public Task<List<GenerationJob>> GetByRevisionAsync(Guid revisionId, CancellationToken ct = default)
        => _db.GenerationJobs
            .Where(j => j.RevisionId == revisionId)
            .OrderBy(j => j.Language)
            .ToListAsync(ct);

    public Task<GenerationJob?> GetByIdAsync(Guid jobId, CancellationToken ct = default)
        => _db.GenerationJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);

    public async Task AddRangeAsync(IEnumerable<GenerationJob> jobs, CancellationToken ct = default)
        => await _db.GenerationJobs.AddRangeAsync(jobs, ct);

    public Task<bool> HasActiveJobsAsync(Guid revisionId, CancellationToken ct = default)
        => _db.GenerationJobs.AnyAsync(
            j => j.RevisionId == revisionId &&
                (j.Status == JobStatus.Queued || j.Status == JobStatus.Processing), ct);
}
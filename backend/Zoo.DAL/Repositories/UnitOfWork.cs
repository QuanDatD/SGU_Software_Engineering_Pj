using Zoo.DAL.Data;
using Zoo.DAL.Interfaces;

namespace Zoo.DAL.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ZooDbContext _db;
    public UnitOfWork(ZooDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        await operation(ct);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
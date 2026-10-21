using Microsoft.EntityFrameworkCore;
using Zoo.DAL.Data;
using Zoo.DAL.Entities;
using Zoo.DAL.Interfaces;

namespace Zoo.DAL.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly ZooDbContext _db;
    public AdminRepository(ZooDbContext db) => _db = db;

    public Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => _db.AdminUsers.FirstOrDefaultAsync(x => x.Username == username && x.IsActive, ct);

    public Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.AdminUsers.FirstOrDefaultAsync(x => x.Id == id && x.IsActive, ct);
}
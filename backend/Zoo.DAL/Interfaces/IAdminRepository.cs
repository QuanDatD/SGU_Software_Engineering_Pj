using Zoo.DAL.Entities;

namespace Zoo.DAL.Interfaces;

public interface IAdminRepository
{
    Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
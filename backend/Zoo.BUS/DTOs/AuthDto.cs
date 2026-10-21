using Zoo.DAL.Entities;

namespace Zoo.DAL.Interfaces;

public interface IAdminRepository
{
    Task<AdminUser?> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default);
}
using Zoo.BUS.DTOs;

namespace Zoo.BUS.Interfaces;

public interface IAuthService
{
    Task<AdminResponse?> ValidateCredentialsAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}
using Microsoft.AspNetCore.Identity;
using Zoo.BUS.DTOs;
using Zoo.BUS.Interfaces;
using Zoo.DAL.Entities;
using Zoo.DAL.Interfaces;

namespace Zoo.BUS.Services;

public sealed class AuthService : IAuthService
{
    private readonly IAdminRepository _adminRepository;
    private readonly IPasswordHasher<AdminUser> _passwordHasher;

    public AuthService(
        IAdminRepository adminRepository,
        IPasswordHasher<AdminUser> passwordHasher)
    {
        _adminRepository = adminRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<AdminResponse?> ValidateCredentialsAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrEmpty(request.Password))
        {
            return null;
        }

        var username = request.Username.Trim();

        var admin = await _adminRepository.GetByUsernameAsync(
            username,
            cancellationToken);

        if (admin is null ||
            !admin.IsActive ||
            admin.Role != "Admin")
        {
            return null;
        }

        var result = _passwordHasher.VerifyHashedPassword(
            admin,
            admin.PasswordHash,
            request.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        // Success và SuccessRehashNeeded đều là mật khẩu đúng.
        // Việc nâng cấp hash sẽ nối thêm khi DAL có hàm cập nhật.

        return new AdminResponse
        {
            Id = admin.Id,
            Username = admin.Username,
            Role = admin.Role
        };
    }
}
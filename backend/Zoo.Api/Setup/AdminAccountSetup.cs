using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zoo.DAL.Data;
using Zoo.DAL.Entities;

namespace Zoo.Api.Setup;

public static class AdminAccountSetup
{
    public static async Task RunAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        var username =
            configuration["BootstrapAdmin:Username"]?.Trim();

        var password =
            configuration["BootstrapAdmin:Password"];

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Chưa cấu hình tài khoản admin trong User Secrets.");
        }

        if (password.Length < 12)
        {
            throw new InvalidOperationException(
                "Mật khẩu cần ít nhất 12 ký tự.");
        }

        using var scope = services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<ZooDbContext>();

        var passwordHasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher<AdminUser>>();

        var exists = await db.AdminUsers
            .AnyAsync(x => x.Username == username);

        if (exists)
        {
            Console.WriteLine(
                "Tài khoản đã tồn tại. Không thay đổi dữ liệu.");
            return;
        }

        var admin = new AdminUser
        {
            Username = username,
            Role = "Admin",
            IsActive = true
        };

        admin.PasswordHash =
            passwordHasher.HashPassword(admin, password);

        db.AdminUsers.Add(admin);

        await db.SaveChangesAsync();

        Console.WriteLine("Đã tạo tài khoản admin thành công.");
    }
}        
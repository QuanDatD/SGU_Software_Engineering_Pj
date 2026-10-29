using Microsoft.EntityFrameworkCore;
using Zoo.DAL.Entities;

namespace Zoo.DAL.Data;

public static class DbSeeder
{
    /// <param name="hashPassword">
    /// Hàm hash mật khẩu được inject từ tầng trên (Zoo.Api) để tránh phụ thuộc
    /// vào Microsoft.AspNetCore.Identity trong Zoo.DAL.
    /// Dùng IPasswordHasher&lt;AdminUser&gt;.HashPassword từ Program.cs.
    /// </param>
    public static async Task SeedAsync(ZooDbContext db, Func<AdminUser, string, string> hashPassword)
    {
        await db.Database.MigrateAsync();

        // 1. Seed Admin mặc định (admin / Admin@123)
        // hashPassword dùng IPasswordHasher<AdminUser> (PBKDF2) — khớp với AuthService
        if (!await db.AdminUsers.AnyAsync())
        {
            var admin = new AdminUser
            {
                Username = "admin",
                Role = "Admin",
                IsActive = true
            };
            admin.PasswordHash = hashPassword(admin, "Admin@123");
            db.AdminUsers.Add(admin);
        }

        // 2. Seed các khu Thảo Cầm Viên mẫu với tọa độ thật tại Quận 1, TP.HCM
        if (!await db.Places.AnyAsync())
        {
            db.Places.AddRange(
                new Place
                {
                    Code = "MAIN_GATE",
                    Category = "facility",
                    Latitude = 10.78754,
                    Longitude = 106.70529,
                    RadiusMeters = 25,
                    MapX = 120,
                    MapY = 420,
                    Priority = 1,
                    ImageUrl = "/images/entrance.jpg",
                    IsActive = true
                },
                new Place
                {
                    Code = "ASIAN_ELEPHANT",
                    Category = "animal",
                    Latitude = 10.78815,
                    Longitude = 106.70612,
                    RadiusMeters = 20,
                    MapX = 280,
                    MapY = 310,
                    Priority = 2,
                    ImageUrl = "/images/elephant.jpg",
                    IsActive = true
                },
                new Place
                {
                    Code = "GIRAFFE_ZONE",
                    Category = "animal",
                    Latitude = 10.78892,
                    Longitude = 106.70655,
                    RadiusMeters = 20,
                    MapX = 360,
                    MapY = 240,
                    Priority = 2,
                    ImageUrl = "/images/giraffe.jpg",
                    IsActive = true
                },
                new Place
                {
                    Code = "ANCIENT_TREE",
                    Category = "plant",
                    Latitude = 10.78795,
                    Longitude = 106.70580,
                    RadiusMeters = 18,
                    MapX = 210,
                    MapY = 350,
                    Priority = 1,
                    ImageUrl = "/images/ancient-tree.jpg",
                    IsActive = true
                }
            );
        }

        // 3. Seed nhãn giao diện cơ bản cho 5 ngôn ngữ
        if (!await db.SiteTexts.AnyAsync())
        {
            db.SiteTexts.AddRange(
                new SiteText { Key = "app.title", Language = "vi", Value = "Thuyết Minh Tự Động Thảo Cầm Viên" },
                new SiteText { Key = "app.title", Language = "en", Value = "Saigon Zoo Audio Guide" },
                new SiteText { Key = "app.title", Language = "zh", Value = "西贡动植物园语音导览" },
                new SiteText { Key = "app.title", Language = "ko", Value = "사이공 동물원 오디오 가이드" },
                new SiteText { Key = "app.title", Language = "ja", Value = "サイゴン動植物園オーディオガイド" }
            );
        }

        await db.SaveChangesAsync();
    }
}

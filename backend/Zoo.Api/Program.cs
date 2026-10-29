using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zoo.Api.Setup;using Zoo.BUS.Interfaces;
using Zoo.BUS.Services;
using Zoo.DAL.Data;
using Zoo.DAL.Entities;
using Zoo.DAL.Interfaces;
using Zoo.DAL.Repositories;
using Zoo.DAL.Integrations.Azure;
using Zoo.DAL.Storage;



using System.Security.Claims;



var builder = WebApplication.CreateBuilder(args);

// 1. Controller và bộ lọc CSRF.
builder.Services.AddControllersWithViews();
builder.Services.AddOpenApi();

// 2. Kết nối SQL Server.
// Có thể ghi đè bằng cấu hình DefaultConnection của máy bạn.
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Database=SaigonZooDb;"
       + "Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddDbContext<ZooDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// 3. Đăng ký DAL.
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IPlaceRepository, PlaceRepository>();
builder.Services.AddScoped<INarrationRepository, NarrationRepository>();
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<ISiteTextRepository, SiteTextRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// 4. Cấu hình Azure và kho file.
builder.Services.Configure<TranslatorOptions>(
    builder.Configuration.GetSection("TranslatorOptions"));

builder.Services.Configure<SpeechOptions>(
    builder.Configuration.GetSection("SpeechOptions"));

builder.Services.Configure<MediaStorageOptions>(
    builder.Configuration.GetSection("MediaStorageOptions"));

builder.Services.PostConfigure<MediaStorageOptions>(options =>
{
    options.RootPath = Path.GetFullPath(
        options.RootPath,
        builder.Environment.ContentRootPath);
});

builder.Services.AddScoped<IMediaStorage, LocalMediaStorage>();

builder.Services.AddHttpClient<
    ITranslationClient,
    AzureTranslationClient>();

builder.Services.AddHttpClient<
    ISpeechClient,
    AzureSpeechClient>();

// 5. Đăng ký BUS.
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGeofenceService, GeofenceService>();
builder.Services.AddScoped<INarrationService, NarrationService>();
builder.Services.AddScoped<IQrVisitService, QrVisitService>();
builder.Services.AddScoped<IReportingService, ReportingService>();

builder.Services.AddScoped<PlaceEditingService>();
builder.Services.AddScoped<IPlaceService, PlaceService>();
builder.Services.AddScoped<IAdminPlaceService, PlaceService>();
builder.Services.AddScoped<Zoo.Api.Filters.ContentWriteFilter>();

builder.Services.AddScoped<
    IPasswordHasher<AdminUser>,
    PasswordHasher<AdminUser>>();

// 6. Cho phép frontend local gọi API kèm cookie.
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// 7. Đăng nhập bằng cookie.
var securePolicy = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;

builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Zoo.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = securePolicy;

        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = false;

        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;

            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode =
                StatusCodes.Status403Forbidden;

            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

// 8. CSRF cho đăng nhập, đăng xuất và thao tác quản trị.
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";

    options.Cookie.Name = "Zoo.Csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = securePolicy;
});

// Chỉ Build một lần, sau tất cả đăng ký service.
var app = builder.Build();

if (args.Contains("--seed-admin"))
{
    if (!app.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "Chỉ chạy khởi tạo admin trong môi trường Development.");
    }

    await AdminAccountSetup.RunAsync(
        app.Services,
        app.Configuration);

    return;
}

// Tự động migrate và seed dữ liệu mẫu khi khởi động
using (var seedScope = app.Services.CreateScope())
{
    var seedDb = seedScope.ServiceProvider.GetRequiredService<ZooDbContext>();
    var hasher = seedScope.ServiceProvider.GetRequiredService<IPasswordHasher<AdminUser>>();
    await Zoo.DAL.Data.DbSeeder.SeedAsync(seedDb, (user, pwd) => hasher.HashPassword(user, pwd));

    // BR-27: phục hồi job bị kẹt Processing sau khi backend restart
    // Job nào Processing mà LockedUntilUtc đã qua hoặc null → reset về Queued
    var now = DateTime.UtcNow;
    var stuckJobs = await seedDb.GenerationJobs
        .Where(j => j.Status == Zoo.DAL.Enums.JobStatus.Processing
                    && (j.LockedUntilUtc == null || j.LockedUntilUtc < now))
        .ToListAsync();

    foreach (var j in stuckJobs)
    {
        j.Status = Zoo.DAL.Enums.JobStatus.Queued;
        j.LastError = "Job bị gián đoạn khi backend khởi động lại. Tạo lại audio để tiếp tục.";
        j.UpdatedAt = now;
    }

    if (stuckJobs.Count > 0)
    {
        await seedDb.SaveChangesAsync();
        app.Logger.LogWarning("Đã reset {Count} job bị kẹt sang Queued.", stuckJobs.Count);
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHttpsRedirection();
}

var storageOptions = builder.Configuration.GetSection("MediaStorageOptions").Get<MediaStorageOptions>() ?? new MediaStorageOptions();
var resolvedStoragePath = Path.GetFullPath(storageOptions.RootPath, builder.Environment.ContentRootPath);

if (!Directory.Exists(resolvedStoragePath))
{
    Directory.CreateDirectory(resolvedStoragePath);
}

Directory.CreateDirectory(Path.Combine(resolvedStoragePath, "images"));
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(Path.Combine(resolvedStoragePath, "images")),
    RequestPath = "/storage/images"
});
app.UseRouting();
app.UseCors("LocalFrontend");
app.UseAuthentication();

// Chỉ bật khi chạy Development và có tham số --demo-admin.
if (app.Environment.IsDevelopment() && args.Contains("--demo-admin"))
{
    // Dùng tài khoản thật để CreatedByAdminId vẫn liên kết đúng database.
    using var scope = app.Services.CreateScope();

    var db = scope.ServiceProvider.GetRequiredService<ZooDbContext>();

    var demoAdmin = await db.AdminUsers
        .AsNoTracking()
        .Where(a => a.IsActive && a.Role == "Admin")
        .OrderBy(a => a.CreatedAt)
        .FirstOrDefaultAsync();

    if (demoAdmin is null)
    {
        throw new InvalidOperationException(
            "Chế độ demo cần ít nhất một tài khoản Admin đang hoạt động.");
    }

    var demoAdminId = demoAdmin.Id.ToString();
    var demoUsername = demoAdmin.Username;

    app.Logger.LogWarning(
        "DEMO ADMIN đang bật: truy cập quản trị không cần mật khẩu.");

    app.Use(async (context, next) =>
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, demoAdminId),
            new Claim(ClaimTypes.Name, demoUsername),
            new Claim(ClaimTypes.Role, "Admin")
        };

        context.User = new ClaimsPrincipal(
            new ClaimsIdentity(claims, "DemoAdmin"));

        await next();
    });
}

app.UseAuthorization();
app.MapControllers();

app.Run();
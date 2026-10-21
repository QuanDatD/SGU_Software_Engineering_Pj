<<<<<<< HEAD
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zoo.BUS.Interfaces;
using Zoo.BUS.Services;
using Zoo.DAL.Entities;
using Zoo.DAL.Data;
using Zoo.DAL.Interfaces;
using Zoo.DAL.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Chỉ bật dữ liệu mẫu trong môi trường Development.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IPlaceService, DemoPlaceService>();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("LocalFrontend", policy =>
        {
            policy
                .WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod();
                .AllowCredentials();
        });
    });
}
else
{
    throw new InvalidOperationException(
        "Chưa cấu hình IPlaceService dùng database. " +
        "Không được triển khai bản dữ liệu mẫu lên production.");
}

builder.Services.AddScoped<IGeofenceService, GeofenceService>();


builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<
    IPasswordHasher<AdminUser>,
    PasswordHasher<AdminUser>>();

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

        // API trả mã lỗi, không chuyển hướng sang trang HTML.
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

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";

    options.Cookie.Name = "Zoo.Csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = securePolicy;
});

var app = builder.Build();

// Đăng ký DAL: lấy tài khoản từ database.
builder.Services.AddScoped<IAdminRepository, AdminRepository>();

// Đăng ký BUS: kiểm tra thông tin đăng nhập.
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();
app.UseRouting();

=======
using Microsoft.EntityFrameworkCore;
using Zoo.DAL.Data;
using Zoo.DAL.Interfaces;
using Zoo.DAL.Repositories;
using Zoo.DAL.Integrations.Azure;
using Zoo.DAL.Storage;

var builder = WebApplication.CreateBuilder(args);

// 1. Kết nối CSDL SQL Server
builder.Services.AddDbContext<ZooDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Server=localhost;Database=SaigonZooDb;Trusted_Connection=True;TrustServerCertificate=True;"));

// 2. Nạp cấu hình Options
builder.Services.Configure<TranslatorOptions>(builder.Configuration.GetSection("TranslatorOptions"));
builder.Services.Configure<SpeechOptions>(builder.Configuration.GetSection("SpeechOptions"));
builder.Services.Configure<MediaStorageOptions>(builder.Configuration.GetSection("MediaStorageOptions"));

// 3. Đăng ký Repositories & Storage (DAL)
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IPlaceRepository, PlaceRepository>();
builder.Services.AddScoped<INarrationRepository, NarrationRepository>();
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<ISiteTextRepository, SiteTextRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IMediaStorage, LocalMediaStorage>();

// 4. Đăng ký Azure Clients
builder.Services.AddHttpClient<ITranslationClient, AzureTranslationClient>();
builder.Services.AddHttpClient<ISpeechClient, AzureSpeechClient>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

>>>>>>> origin/main
if (app.Environment.IsDevelopment())
{
    app.UseCors("LocalFrontend");
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

<<<<<<< HEAD
=======
app.UseHttpsRedirection();
app.MapControllers();

>>>>>>> origin/main
app.Run();
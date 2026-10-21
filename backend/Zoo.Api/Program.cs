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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
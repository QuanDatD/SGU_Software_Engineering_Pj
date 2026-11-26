using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
using Zoo.DAL.Data;
using Zoo.DAL.Interfaces;

namespace Zoo.Tests.Integration;

public sealed class SqlFactAttribute : FactAttribute
{
    public SqlFactAttribute([CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = 0) : base(sourceFilePath, sourceLineNumber)
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ZOO_TEST_SQL")))
            Skip = "Set ZOO_TEST_SQL to run against a disposable SQL Server database.";
    }
}

[CollectionDefinition("SQL-backed API")]
public sealed class SqlApiCollection : ICollectionFixture<SqlApiFixture>;

public sealed class SqlApiFixture : WebApplicationFactory<Program>
{
    private readonly string connection = new SqlConnectionStringBuilder(
        Environment.GetEnvironmentVariable("ZOO_TEST_SQL")
        ?? throw new InvalidOperationException("ZOO_TEST_SQL is required."))
    {
        InitialCatalog = $"ZooCi_{Guid.NewGuid():N}"
    }.ConnectionString;

    private readonly string mediaPath = Path.Combine(Path.GetTempPath(), $"ZooCi_{Guid.NewGuid():N}");
    internal AzureDouble Azure { get; } = new();

    internal ZooDbContext OpenDatabase() =>
        new(new DbContextOptionsBuilder<ZooDbContext>().UseSqlServer(connection).Options);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", connection);
        builder.UseSetting("MediaStorageOptions:RootPath", mediaPath);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ITranslationClient>();
            services.RemoveAll<ISpeechClient>();
            services.AddSingleton<ITranslationClient>(Azure);
            services.AddSingleton<ISpeechClient>(Azure);
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        try
        {
            await using var db = OpenDatabase();
            await db.Database.EnsureDeletedAsync();
        }
        finally
        {
            if (Directory.Exists(mediaPath))
                Directory.Delete(mediaPath, recursive: true);
        }
    }
}

internal sealed class AzureDouble : ITranslationClient, ISpeechClient
{
    internal Func<string, CancellationToken, Task>? BeforeSynthesis { get; set; }
    internal Func<string, CancellationToken, Task>? BeforeTranslation { get; set; }
    internal string? FailedLanguage { get; set; }

    public async Task<string> TranslateAsync(string text, string fromLanguage, string toLanguage,
        CancellationToken ct = default)
    {
        if (BeforeTranslation is not null)
            await BeforeTranslation(toLanguage, ct);
        return $"{toLanguage}: {text}";
    }

    public async Task<byte[]> SynthesizeAsync(string text, string language, CancellationToken ct = default)
    {
        if (BeforeSynthesis is not null)
            await BeforeSynthesis(language, ct);
        if (language == FailedLanguage)
            throw new HttpRequestException("Simulated provider failure.");
        return [1, 2, 3];
    }
}

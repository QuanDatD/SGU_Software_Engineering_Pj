using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Zoo.DAL.Interfaces;
using Zoo.Tests.Integration;

namespace Zoo.Tests.BrowserHost;

internal static class Program
{
    public static async Task Main()
    {
        var sql = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ZOO_E2E_SQL")
            ?? throw new InvalidOperationException("Set ZOO_E2E_SQL to a disposable ZooCi_ database."));
        if (!sql.InitialCatalog.StartsWith("ZooCi_", StringComparison.Ordinal))
            throw new InvalidOperationException("Playwright requires a disposable ZooCi_ database.");
        var master = new SqlConnectionStringBuilder(sql.ConnectionString) { InitialCatalog = "master" };
        await using (var connection = new SqlConnection(master.ConnectionString))
        {
            await connection.OpenAsync();
            await using var check = connection.CreateCommand();
            check.CommandText = "SELECT DB_ID(@database)";
            check.Parameters.AddWithValue("@database", sql.InitialCatalog);
            if (await check.ExecuteScalarAsync() is int)
                throw new InvalidOperationException("Playwright requires a new test database for each run.");
            await using var create = connection.CreateCommand();
            create.CommandText = "CREATE DATABASE " + new SqlCommandBuilder().QuoteIdentifier(sql.InitialCatalog);
            await create.ExecuteNonQueryAsync();
        }
        await using var api = new SqlApiFixture(sql, new BrowserSpeech());
        api.UseKestrel(5230);
        api.StartServer();
        var lifetime = api.Services.GetRequiredService<IHostApplicationLifetime>();
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = lifetime.ApplicationStopping.Register(() => stopped.TrySetResult());
        await stopped.Task;
    }
}

internal sealed class BrowserSpeech : ISpeechClient
{
    private static readonly byte[] Preview = CreatePreview();
    private readonly ConcurrentDictionary<string, byte> failed = new();

    public Task<byte[]> SynthesizeAsync(string text, string language, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (language == "en" && text.Contains("CI retry fixture", StringComparison.Ordinal)
            && failed.TryAdd(text, 0))
            throw new HttpRequestException("Simulated speech failure for the first English attempt.");
        return Task.FromResult(Preview);
    }

    private static byte[] CreatePreview()
    {
        const int sampleRate = 8000;
        const int samples = sampleRate * 12;
        const int dataBytes = samples * sizeof(short);
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8);
        writer.Write(36 + dataBytes);
        writer.Write("WAVEfmt "u8);
        writer.Write(16);
        writer.Write((short)1); // PCM
        writer.Write((short)1); // mono
        writer.Write(sampleRate);
        writer.Write(sampleRate * sizeof(short));
        writer.Write((short)sizeof(short));
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(dataBytes);
        for (var sample = 0; sample < samples; sample++)
            writer.Write((short)(1000 * Math.Sin(2 * Math.PI * 440 * sample / sampleRate)));
        return stream.ToArray();
    }
}

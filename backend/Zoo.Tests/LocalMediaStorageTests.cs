using Microsoft.Extensions.Options;
using Xunit;
using Zoo.DAL.Storage;

namespace Zoo.Tests;

public sealed class LocalMediaStorageTests : IDisposable
{
    private readonly DirectoryInfo tempDirectory = Directory.CreateTempSubdirectory("ZooAudioTests_");
    private string StorageRoot => Path.Combine(tempDirectory.FullName, "storage");
    private LocalMediaStorage Storage => new(Options.Create(new MediaStorageOptions { RootPath = StorageRoot }));

    [Fact]
    public async Task NewAudioIsWrittenAndReadFromTheSiblingDirectory()
    {
        var placeId = Guid.NewGuid();
        var revisionId = Guid.NewGuid();
        byte[] audio = [1, 2, 3];
        var cancellationToken = TestContext.Current.CancellationToken;

        var relativePath = await Storage.SaveAudioAsync(placeId, revisionId, "EN", audio, cancellationToken);

        Assert.StartsWith($"audio2/{placeId}/{revisionId}/en-", relativePath);
        Assert.EndsWith(".mp3", relativePath);
        var fullPath = Path.Combine(tempDirectory.FullName, relativePath);
        Assert.Equal(audio, await File.ReadAllBytesAsync(fullPath, cancellationToken));
        await using var stream = Assert.IsType<FileStream>(await Storage.OpenReadAudioAsync(relativePath, cancellationToken));
        Assert.Equal(Path.GetFullPath(fullPath), stream.Name);
    }

    [Fact]
    public async Task LegacyAudioIsReadFromStorage()
    {
        const string relativePath = "audio/legacy.mp3";
        var fullPath = Path.Combine(StorageRoot, relativePath);
        var cancellationToken = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllBytesAsync(fullPath, [1, 2, 3], cancellationToken);

        await using var stream = Assert.IsType<FileStream>(await Storage.OpenReadAudioAsync(relativePath, cancellationToken));
        Assert.Equal(Path.GetFullPath(fullPath), stream.Name);
        Assert.Null(await Storage.OpenReadAudioAsync("audio2/../storage/audio/legacy.mp3", cancellationToken));
    }

    public void Dispose() => tempDirectory.Delete(recursive: true);
}

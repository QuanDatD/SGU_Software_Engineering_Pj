using Microsoft.Extensions.Options;
using Zoo.DAL.Interfaces;

namespace Zoo.DAL.Storage;

public class LocalMediaStorage : IMediaStorage
{
    private readonly MediaStorageOptions _options;

    public LocalMediaStorage(IOptions<MediaStorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string> SaveAudioAsync(Guid placeId, Guid revisionId, string language, byte[] audioBytes, CancellationToken ct = default)
    {
        var relativePath = Path.Combine("audio", placeId.ToString(), revisionId.ToString(), $"{language.ToLowerInvariant()}-{Guid.NewGuid():N}.mp3")
            .Replace('\\', '/');

        var fullPath = Path.GetFullPath(Path.Combine(_options.RootPath, relativePath));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await File.WriteAllBytesAsync(fullPath, audioBytes, ct);
        return relativePath;
    }

    public Task<Stream?> OpenReadAudioAsync(string relativePath, CancellationToken ct = default)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_options.RootPath, relativePath));
        if (!fullPath.StartsWith(Path.GetFullPath(_options.RootPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<Stream?>(null);
        if (!File.Exists(fullPath))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public async Task<string> SaveImageAsync(string fileName, Stream imageStream, CancellationToken ct = default)
    {
        var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var relativePath = Path.Combine("images", safeName).Replace('\\', '/');
        var fullPath = Path.GetFullPath(Path.Combine(_options.RootPath, relativePath));

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await imageStream.CopyToAsync(fileStream, ct);

        return relativePath;
    }
}
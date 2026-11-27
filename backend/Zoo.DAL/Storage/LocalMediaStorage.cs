using Microsoft.Extensions.Options;
using Zoo.DAL.Interfaces;

namespace Zoo.DAL.Storage;

public class LocalMediaStorage : IMediaStorage
{
    private const string GeneratedAudioFolder = "audio2";
    private const string GeneratedAudioPrefix = GeneratedAudioFolder + "/";
    private readonly MediaStorageOptions _options;
    private string GeneratedAudioRoot => Path.GetFullPath(Path.Combine(_options.RootPath, "..", GeneratedAudioFolder));

    public LocalMediaStorage(IOptions<MediaStorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string> SaveAudioAsync(Guid placeId, Guid revisionId, string language, byte[] audioBytes, CancellationToken ct = default)
    {
        var filePath = Path.Combine(placeId.ToString(), revisionId.ToString(), $"{language.ToLowerInvariant()}-{Guid.NewGuid():N}.mp3");

        var fullPath = Path.Combine(GeneratedAudioRoot, filePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await File.WriteAllBytesAsync(fullPath, audioBytes, ct);
        return GeneratedAudioPrefix + filePath.Replace('\\', '/');
    }

    public Task<Stream?> OpenReadAudioAsync(string relativePath, CancellationToken ct = default)
    {
        var generated = relativePath.StartsWith(GeneratedAudioPrefix, StringComparison.Ordinal);
        var root = generated ? GeneratedAudioRoot : Path.GetFullPath(_options.RootPath);
        var filePath = generated ? relativePath[GeneratedAudioPrefix.Length..] : relativePath;
        var fullPath = Path.GetFullPath(Path.Combine(root, filePath));
        var rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!fullPath.StartsWith(rootPrefix, comparison))
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

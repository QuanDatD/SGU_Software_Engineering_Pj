namespace Zoo.DAL.Interfaces;

public interface IMediaStorage
{
    Task<string> SaveAudioAsync(Guid placeId, Guid revisionId, string language, byte[] audioBytes, CancellationToken ct = default);
    Task<Stream?> OpenReadAudioAsync(string relativePath, CancellationToken ct = default);
    Task<string> SaveImageAsync(string fileName, Stream imageStream, CancellationToken ct = default);
}
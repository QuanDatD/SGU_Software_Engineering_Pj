namespace Zoo.DAL.Interfaces;

public interface ISpeechClient
{
    Task<byte[]> SynthesizeAsync(string text, string language, CancellationToken ct = default);
}
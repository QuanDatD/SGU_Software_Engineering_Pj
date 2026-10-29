using System.Security.Cryptography;
using System.Text;
using Zoo.DAL.Entities;
using Zoo.DAL.Enums;
namespace Zoo.BUS.Services;

public static class PublicationRules
{
    public static readonly string[] Languages = ["vi", "en", "zh", "ko", "ja"];
    public static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    public static bool Ready(NarrationTranslation t) => !string.IsNullOrWhiteSpace(t.Title)
        && !string.IsNullOrWhiteSpace(t.ScriptText) && t.AudioStatus == AudioStatus.Ready
        && !string.IsNullOrWhiteSpace(t.AudioPath) && t.AudioContentHash == Hash(t.ScriptText);
    public static void Invalidate(NarrationTranslation t)
    {
        t.AudioStatus = AudioStatus.Missing; t.AudioPath = null; t.AudioContentHash = null;
        t.UpdatedAt = DateTime.UtcNow;
    }
}

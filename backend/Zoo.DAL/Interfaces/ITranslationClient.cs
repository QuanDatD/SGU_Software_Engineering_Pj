namespace Zoo.DAL.Interfaces;

public interface ITranslationClient
{
    Task<string> TranslateAsync(string text, string fromLanguage, string toLanguage, CancellationToken ct = default);
}
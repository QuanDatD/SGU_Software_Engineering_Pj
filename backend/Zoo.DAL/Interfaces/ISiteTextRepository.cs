using Zoo.DAL.Entities;

namespace Zoo.DAL.Interfaces;

public interface ISiteTextRepository
{
    Task<List<SiteText>> GetByLanguageAsync(string language, CancellationToken ct = default);
    Task UpsertAsync(string key, string language, string value, CancellationToken ct = default);
}
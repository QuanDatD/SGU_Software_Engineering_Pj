using Zoo.BUS.DTOs;
using Zoo.BUS.Interfaces;
namespace Zoo.BUS.Services;
public sealed class NarrationService(IPlaceService places) : INarrationService
{
    public async Task<NarrationDto?> GetNarrationAsync(Guid placeId, string language, CancellationToken ct = default)
    {
        var p = await places.GetPublishedByIdAsync(placeId, language, ct);
        if (p == null || !p.Script.TryGetValue(language, out var script)) return null;
        var audio = p.AudioUrl.GetValueOrDefault(language);
        return new NarrationDto { PlaceId = p.Id, PlaceCode = p.Code, Language = language,
            Title = p.Name.GetValueOrDefault(language, ""), Description = p.Desc.GetValueOrDefault(language, ""),
            ScriptText = script, AudioUrl = audio, HasAudio = audio != null };
    }
}

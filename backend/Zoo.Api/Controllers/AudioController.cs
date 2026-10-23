using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zoo.BUS.Services;
using Zoo.DAL.Data;
using Zoo.DAL.Enums;
using Zoo.DAL.Interfaces;
namespace Zoo.Api.Controllers;
[ApiController]
public sealed class AudioController(ZooDbContext db, IMediaStorage storage) : ControllerBase
{
    [HttpGet("api/places/{placeId:guid}/audio/{lang}")]
    public async Task<IActionResult> Public(Guid placeId, string lang, Guid revisionId, CancellationToken ct)
    {
        var place = await db.Places.AsNoTracking().Include(p => p.PublishedRevision).ThenInclude(r => r!.Translations)
            .FirstOrDefaultAsync(p => p.Id == placeId && p.IsActive, ct);
        var revision = place?.PublishedRevision;
        if (revision == null || revision.Id != revisionId || revision.PlaceId != placeId || revision.Status != RevisionStatus.Published)
            return NotFound();
        return await StreamAudio(revision.Translations.FirstOrDefault(t => t.Language == lang), ct);
    }
    [Authorize(Roles = "Admin")]
    [HttpGet("api/admin/revisions/{revisionId:guid}/audio/{lang}")]
    public async Task<IActionResult> Preview(Guid revisionId, string lang, CancellationToken ct)
        => await StreamAudio(await db.NarrationTranslations.AsNoTracking().FirstOrDefaultAsync(t => t.RevisionId == revisionId && t.Language == lang, ct), ct);
    private async Task<IActionResult> StreamAudio(Zoo.DAL.Entities.NarrationTranslation? translation, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        if (translation == null || !PublicationRules.Ready(translation)) return NotFound();
        var stream = await storage.OpenReadAudioAsync(translation.AudioPath!, ct);
        return stream == null ? NotFound() : File(stream, "audio/mpeg", enableRangeProcessing: true);
    }
}

using Microsoft.EntityFrameworkCore;
using Zoo.BUS.DTOs;
using Zoo.DAL.Data;
using Zoo.DAL.Entities;
using Zoo.DAL.Enums;
using Zoo.DAL.Interfaces;
namespace Zoo.BUS.Services;
public sealed class PlaceEditingService(ZooDbContext db, IMediaStorage storage)
{
    public async Task<Guid?> SaveAsync(Guid? id, SavePlaceRequest input, string? imageName, Stream? image, CancellationToken ct)
    {
        var place = id.HasValue ? await db.Places.Include(p => p.Revisions).ThenInclude(r => r.Translations)
            .FirstOrDefaultAsync(p => p.Id == id, ct) : new Place { Code = "ZONE_" + Guid.NewGuid().ToString("N") };
        if (place == null) return null;
        if (!id.HasValue) db.Places.Add(place);
        place.Category = input.Category; place.Latitude = input.Latitude; place.Longitude = input.Longitude;
        place.RadiusMeters = input.RadiusMeters; place.MapX = input.MapX; place.MapY = input.MapY;
        place.Priority = input.Priority; place.UpdatedAt = DateTime.UtcNow;
        if (image != null) place.ImageUrl = "/storage/" + await storage.SaveImageAsync(imageName!, image, ct);
        var latest = place.Revisions.OrderByDescending(r => r.Version).FirstOrDefault();
        var revision = latest?.Status == RevisionStatus.Draft ? latest : null;
        if (revision == null)
        {
            revision = new NarrationRevision { PlaceId = place.Id, Version = (latest?.Version ?? 0) + 1 };
            revision.Translations.Add(new NarrationTranslation { RevisionId = revision.Id, Language = "vi",
                ScriptText = latest?.Translations.FirstOrDefault(t => t.Language == "vi")?.ScriptText ?? "" });
            place.Revisions.Add(revision);
            db.NarrationRevisions.Add(revision);
        }
        var vi = revision.Translations.FirstOrDefault(t => t.Language == "vi");
        if (vi == null) { vi = new NarrationTranslation { RevisionId = revision.Id, Language = "vi" }; revision.Translations.Add(vi); db.NarrationTranslations.Add(vi); }
        if (vi.Title != input.NameVi.Trim() || vi.Description != input.DescriptionVi.Trim())
        {
            vi.Title = input.NameVi.Trim(); vi.Description = input.DescriptionVi.Trim();
            foreach (var tr in revision.Translations)
            {
                PublicationRules.Invalidate(tr);
                if (tr.Language != "vi") { tr.ScriptText = ""; tr.IsEditedByAdmin = false; }
            }
            var jobs = await db.GenerationJobs.Where(j => j.RevisionId == revision.Id).ToListAsync(ct);
            foreach (var job in jobs) job.Status = JobStatus.Queued;
        }
        await db.SaveChangesAsync(ct);
        return place.Id;
    }
    public async Task<bool> HideAsync(Guid id, CancellationToken ct)
    {
        var place = await db.Places.FindAsync([id], ct);
        if (place == null) return false;
        place.IsActive = false; place.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct); return true;
    }
}

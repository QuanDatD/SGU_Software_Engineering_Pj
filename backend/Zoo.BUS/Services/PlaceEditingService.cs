using Microsoft.EntityFrameworkCore;
using Zoo.BUS.DTOs;
using Zoo.DAL.Data;
using Zoo.DAL.Entities;
using Zoo.DAL.Enums;
using Zoo.DAL.Interfaces;

namespace Zoo.BUS.Services;

public enum DeletePlaceResult { Ok, NotFound, HasRevisions }

public sealed class PlaceEditingService(ZooDbContext db, IMediaStorage storage)
{
    public async Task<(Guid? PlaceId, bool Processing)> SaveAsync(
        Guid? id, Guid adminId, SavePlaceRequest input, string? imageName, Stream? image, CancellationToken ct)
    {
        var place = id.HasValue
            ? await db.Places.Include(p => p.Revisions).ThenInclude(r => r.Translations)
                .FirstOrDefaultAsync(p => p.Id == id, ct)
            : new Place { Code = "ZONE_" + Guid.NewGuid().ToString("N") };
        if (place == null) return (null, false);

        var latest = place.Revisions.OrderByDescending(r => r.Version).FirstOrDefault();
        var previousVi = latest?.Translations.FirstOrDefault(t => t.Language == "vi");
        var title = input.NameVi.Trim();
        var description = input.DescriptionVi.Trim();
        var contentChanged = previousVi == null || previousVi.Title != title || previousVi.Description != description;
        if (contentChanged && latest?.Status == RevisionStatus.Draft &&
            await db.GenerationJobs.AnyAsync(j => j.RevisionId == latest.Id && j.Status == JobStatus.Processing, ct))
            return (place.Id, true);

        if (!id.HasValue) db.Places.Add(place);
        var now = DateTime.UtcNow;
        place.Category = input.Category;
        place.Latitude = input.Latitude;
        place.Longitude = input.Longitude;
        place.RadiusMeters = input.RadiusMeters;
        place.MapX = input.MapX;
        place.MapY = input.MapY;
        place.Priority = input.Priority;
        place.UpdatedAt = now;
        if (image != null)
            place.ImageUrl = "/storage/" + await storage.SaveImageAsync(imageName!, image, ct);

        if (contentChanged)
        {
            var draft = latest?.Status == RevisionStatus.Draft ? latest : null;
            var vi = draft == null ? null : previousVi;
            if (draft == null)
            {
                draft = new NarrationRevision
                {
                    PlaceId = place.Id,
                    Version = (latest?.Version ?? 0) + 1,
                    CreatedByAdminId = adminId
                };
                db.NarrationRevisions.Add(draft);
            }
            if (vi == null)
            {
                var script = previousVi?.ScriptText ?? "";
                vi = new NarrationTranslation
                {
                    RevisionId = draft.Id,
                    Language = "vi",
                    ScriptText = script,
                    ContentHash = PublicationRules.Hash(script)
                };
                db.NarrationTranslations.Add(vi);
            }

            vi.Title = title;
            vi.Description = description;
            vi.UpdatedAt = now;
            draft.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);
        return (place.Id, false);
    }

    public async Task<bool> HideAsync(Guid id, CancellationToken ct)
    {
        var place = await db.Places.FindAsync([id], ct);
        if (place == null) return false;
        place.IsActive = false; place.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct); return true;
    }

    public async Task<bool> ShowAsync(Guid id, CancellationToken ct)
    {
        var place = await db.Places.FindAsync([id], ct);
        if (place == null) return false;
        place.IsActive = true; place.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct); return true;
    }

    /// <summary>
    /// UC05.3 — Xóa điểm chỉ khi không có phiên bản thuyết minh (BR-25).
    /// </summary>
    public async Task<DeletePlaceResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        var place = await db.Places
            .Include(p => p.Revisions)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (place == null) return DeletePlaceResult.NotFound;

        if (place.Revisions.Any())
            return DeletePlaceResult.HasRevisions;

        db.Places.Remove(place);
        await db.SaveChangesAsync(ct);
        return DeletePlaceResult.Ok;
    }
}
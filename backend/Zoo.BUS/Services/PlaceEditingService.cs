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
    public async Task<Guid?> SaveAsync(Guid? id, SavePlaceRequest input, string? imageName, Stream? image, CancellationToken ct)
    {
        var place = id.HasValue ? await db.Places.Include(p => p.Revisions).ThenInclude(r => r.Translations)
            .FirstOrDefaultAsync(p => p.Id == id, ct) : new Place { Code = "ZONE_" + Guid.NewGuid().ToString("N") };
        if (place == null) return null;

        if (!id.HasValue) db.Places.Add(place);
        
        // Store old Place state for comparison
        var oldCategory = place.Category;
        var oldLatitude = place.Latitude;
        var oldLongitude = place.Longitude;
        var oldRadiusMeters = place.RadiusMeters;
        var oldMapX = place.MapX;
        var oldMapY = place.MapY;
        var oldPriority = place.Priority;
        var oldImageUrl = place.ImageUrl;

        // Update Place fields
        place.Category = input.Category;
        place.Latitude = input.Latitude;
        place.Longitude = input.Longitude;
        place.RadiusMeters = input.RadiusMeters;
        place.MapX = input.MapX;
        place.MapY = input.MapY;
        place.Priority = input.Priority;
        place.UpdatedAt = DateTime.UtcNow;
        
        if (image != null)
            place.ImageUrl = "/storage/" + await storage.SaveImageAsync(imageName!, image, ct);

        // UC05: Detect nếu chỉ thay GPS, radius, image, priority → skip revision
        var isNewPlace = !id.HasValue;
        var gpsRadiusImageOnly = !isNewPlace && 
            oldCategory == input.Category &&
            oldLatitude == input.Latitude &&
            oldLongitude == input.Longitude &&
            oldRadiusMeters == input.RadiusMeters &&
            oldMapX == input.MapX &&
            oldMapY == input.MapY &&
            oldPriority == input.Priority;

        // UC05: Nếu chỉ GPS/radius/image/priority → update Place, NOT create revision
        if (gpsRadiusImageOnly && id.HasValue)
        {
            await db.SaveChangesAsync(ct);
            return place.Id;
        }

        // Otherwise: create or update Draft revision (NameVi, DescriptionVi changed or new place)
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
        
        // Update Vi translation with new NameVi, DescriptionVi
        vi.Title = input.NameVi.Trim();
        vi.Description = input.DescriptionVi.Trim();
        vi.UpdatedAt = DateTime.UtcNow;
        
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
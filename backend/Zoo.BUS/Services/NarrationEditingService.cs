using Microsoft.EntityFrameworkCore;
using Zoo.BUS.DTOs;
using Zoo.DAL.Data;
using Zoo.DAL.Entities;
using Zoo.DAL.Enums;

namespace Zoo.BUS.Services;

public enum UpdateContentResult { Ok, NotFound, NotDraft, HasProcessingJobs }
public enum DeleteRevisionResult { Ok, NotFound, NotDraft, HasProcessingJobs }

public class NarrationEditingService(ZooDbContext db)
{
    public Task<UpdateContentResult> UpdateViContentAsync(Guid revisionId, NarrationContentRequest input, CancellationToken ct) =>
        UpdateViContentWithReviewAsync(revisionId, input, ct);

    public async Task<UpdateContentResult> UpdateViContentWithReviewAsync(
        Guid revisionId, NarrationContentRequest input, CancellationToken ct)
    {
        var (result, _) = await EditTranslationAsync(revisionId, "vi", input.ScriptText, input.Title, input.Description, ct);
        return result;
    }

    public Task<(UpdateContentResult Result, NarrationTranslation? Translation)> UpdateTranslationAsync(
        Guid revisionId, string language, string text, CancellationToken ct) =>
        EditTranslationAsync(revisionId, language, text, null, null, ct);

    private async Task<(UpdateContentResult Result, NarrationTranslation? Translation)> EditTranslationAsync(
        Guid revisionId, string language, string? script, string? title, string? description, CancellationToken ct)
    {
        var revision = await db.NarrationRevisions.Include(r => r.Place).Include(r => r.Translations)
            .Include(r => r.Jobs).FirstOrDefaultAsync(r => r.Id == revisionId, ct);
        if (revision == null) return (UpdateContentResult.NotFound, null);
        if (revision.Status != RevisionStatus.Draft) return (UpdateContentResult.NotDraft, null);
        if (revision.Jobs.Any(j => j.Status == JobStatus.Processing))
            return (UpdateContentResult.HasProcessingJobs, null);

        var translation = revision.Translations.FirstOrDefault(t => t.Language == language);
        if (translation == null)
        {
            translation = new NarrationTranslation
            {
                RevisionId = revisionId,
                Language = language,
                Title = language == "vi" ? revision.Place.Code : ""
            };
            db.NarrationTranslations.Add(translation);
        }

        translation.Title = title?.Trim() ?? translation.Title;
        translation.Description = description?.Trim() ?? translation.Description;
        if (script != null)
        {
            var previousHash = PublicationRules.Hash(translation.ScriptText);
            var newScript = script.Trim();
            var changed = translation.ScriptText != newScript;
            translation.ScriptText = newScript;
            translation.ContentHash = PublicationRules.Hash(newScript);
            translation.IsEditedByAdmin = true;
            translation.NeedsReview = false;

            if (language != "vi")
            {
                var source = revision.Translations.FirstOrDefault(t => t.Language == "vi");
                translation.SourceContentHash = source == null ? null : PublicationRules.Hash(source.ScriptText);
            }

            if (changed)
            {
                PublicationRules.Invalidate(translation);
                QueueJob(revision.Jobs.FirstOrDefault(j => j.Language == language), JobStage.Synthesize);
                if (language == "vi")
                {
                    foreach (var other in revision.Translations.Where(t => t.Language != "vi" && !string.IsNullOrWhiteSpace(t.ScriptText)))
                    {
                        other.SourceContentHash ??= previousHash;
                        other.NeedsReview = true;
                        other.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }
        }

        translation.UpdatedAt = DateTime.UtcNow;
        revision.UpdatedAt = translation.UpdatedAt;
        await db.SaveChangesAsync(ct);
        return (UpdateContentResult.Ok, translation);
    }

    public async Task<DeleteRevisionResult> DeleteDraftAsync(Guid revisionId, CancellationToken ct)
    {
        var revision = await db.NarrationRevisions.Include(r => r.Jobs)
            .FirstOrDefaultAsync(r => r.Id == revisionId, ct);
        if (revision == null) return DeleteRevisionResult.NotFound;
        if (revision.Status != RevisionStatus.Draft) return DeleteRevisionResult.NotDraft;
        if (revision.Jobs.Any(j => j.Status == JobStatus.Processing)) return DeleteRevisionResult.HasProcessingJobs;

        db.NarrationRevisions.Remove(revision);
        await db.SaveChangesAsync(ct);
        return DeleteRevisionResult.Ok;
    }

    public Task<bool> HasProcessingJobsAsync(Guid revisionId, CancellationToken ct) =>
        db.GenerationJobs.AnyAsync(j => j.RevisionId == revisionId && j.Status == JobStatus.Processing, ct);

    public async Task<UpdateContentResult> ReviewTranslationAsync(
        Guid revisionId, string language, ReviewTranslationRequest request, CancellationToken ct)
    {
        var revision = await db.NarrationRevisions.Include(r => r.Translations).Include(r => r.Jobs)
            .FirstOrDefaultAsync(r => r.Id == revisionId, ct);
        if (revision == null) return UpdateContentResult.NotFound;
        if (revision.Status != RevisionStatus.Draft) return UpdateContentResult.NotDraft;
        if (revision.Jobs.Any(j => j.Status == JobStatus.Processing)) return UpdateContentResult.HasProcessingJobs;

        var translation = revision.Translations.FirstOrDefault(t => t.Language == language);
        var source = revision.Translations.FirstOrDefault(t => t.Language == "vi");
        if (translation == null || source == null) return UpdateContentResult.NotFound;
        if (!translation.NeedsReview) return UpdateContentResult.Ok;

        if (request.Action == "UpdateToNew")
        {
            translation.ScriptText = "";
            translation.ContentHash = PublicationRules.Hash("");
            translation.IsEditedByAdmin = false;
            PublicationRules.Invalidate(translation);
            QueueJob(revision.Jobs.FirstOrDefault(j => j.Language == language), JobStage.Translate);
        }

        translation.SourceContentHash = PublicationRules.Hash(source.ScriptText);
        translation.NeedsReview = false;
        translation.UpdatedAt = DateTime.UtcNow;
        revision.UpdatedAt = translation.UpdatedAt;
        await db.SaveChangesAsync(ct);
        return UpdateContentResult.Ok;
    }

    private static void QueueJob(GenerationJob? job, JobStage stage)
    {
        if (job == null) return;
        job.Status = JobStatus.Queued;
        job.Stage = stage;
        job.LockedUntilUtc = null;
        job.LastError = null;
        job.UpdatedAt = DateTime.UtcNow;
    }
}

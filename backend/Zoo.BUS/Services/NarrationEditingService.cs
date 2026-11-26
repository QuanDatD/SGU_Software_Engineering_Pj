using Microsoft.EntityFrameworkCore;
using Zoo.BUS.DTOs;
using Zoo.DAL.Data;
using Zoo.DAL.Entities;
using Zoo.DAL.Enums;
using Zoo.DAL.Interfaces;

namespace Zoo.BUS.Services;

public enum UpdateContentResult { Ok, NotFound, NotDraft, HasProcessingJobs }
public enum DeleteRevisionResult { Ok, NotFound, NotDraft, HasProcessingJobs }

/// <summary>
/// Service quản lý nội dung narration (UC06)
/// </summary>
public class NarrationEditingService
{
    private readonly ZooDbContext _db;

    public NarrationEditingService(ZooDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Cập nhật content tiếng Việt của phiên bản nháp
    /// </summary>
    public async Task<UpdateContentResult> UpdateViContentAsync(Guid revisionId, NarrationContentRequest input, CancellationToken ct)
    {
        var revision = await _db.NarrationRevisions
            .Include(r => r.Translations)
            .Include(r => r.Jobs)
            .FirstOrDefaultAsync(r => r.Id == revisionId, ct);

        if (revision == null)
            return UpdateContentResult.NotFound;

        // Chỉ cho phép cập nhật bản Draft
        if (revision.Status != RevisionStatus.Draft)
            return UpdateContentResult.NotDraft;

        // Chặn cập nhật nếu có job đang Processing
        if (revision.Jobs.Any(j => j.Status == JobStatus.Processing))
            return UpdateContentResult.HasProcessingJobs;

        // Cập nhật translation Vi
        var viTrans = revision.Translations.FirstOrDefault(t => t.Language == "vi");
        if (viTrans == null)
        {
            viTrans = new NarrationTranslation
            {
                RevisionId = revision.Id,
                Language = "vi",
                UpdatedAt = DateTime.UtcNow
            };
            revision.Translations.Add(viTrans);
        }

        // Cập nhật các trường
        if (!string.IsNullOrWhiteSpace(input.Title))
            viTrans.Title = input.Title;
        
        if (!string.IsNullOrWhiteSpace(input.Description))
            viTrans.Description = input.Description;
        
        if (!string.IsNullOrWhiteSpace(input.ScriptText))
        {
            viTrans.ScriptText = input.ScriptText;
            // Tính hash mới cho script
            viTrans.ContentHash = PublicationRules.Hash(input.ScriptText);
            // Audio chỉ hết hiệu lực khi script thay đổi (invalidate)
            PublicationRules.Invalidate(viTrans);
        }

        viTrans.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return UpdateContentResult.Ok;
    }

    /// <summary>
    /// Cập nhật content tiếng Việt & xử lý translation review
    /// </summary>
    public async Task<UpdateContentResult> UpdateViContentWithReviewAsync(
        Guid revisionId,
        NarrationContentRequest input,
        CancellationToken ct)
    {
        var revision = await _db.NarrationRevisions
            .Include(r => r.Translations)
            .Include(r => r.Jobs)
            .FirstOrDefaultAsync(r => r.Id == revisionId, ct);

        if (revision == null)
            return UpdateContentResult.NotFound;

        if (revision.Status != RevisionStatus.Draft)
            return UpdateContentResult.NotDraft;

        if (revision.Jobs.Any(j => j.Status == JobStatus.Processing))
            return UpdateContentResult.HasProcessingJobs;

        // Get Vi translation
        var viTrans = revision.Translations.FirstOrDefault(t => t.Language == "vi");
        if (viTrans == null)
        {
            viTrans = new NarrationTranslation
            {
                RevisionId = revision.Id,
                Language = "vi"
            };
            revision.Translations.Add(viTrans);
        }

        // Check if script changed
        var oldScriptHash = viTrans.ContentHash;
        var newScriptHash = PublicationRules.Hash(input.ScriptText ?? "");
        var scriptChanged = oldScriptHash != newScriptHash && !string.IsNullOrWhiteSpace(input.ScriptText);

        // Update Vi translation
        viTrans.Title = input.Title ?? viTrans.Title;
        viTrans.Description = input.Description ?? viTrans.Description;
        viTrans.ScriptText = input.ScriptText ?? viTrans.ScriptText;
        viTrans.ContentHash = newScriptHash;
        viTrans.UpdatedAt = DateTime.UtcNow;

        // Nếu script thay đổi, invalidate audio & mark translation review
        if (scriptChanged)
        {
            PublicationRules.Invalidate(viTrans);
            viTrans.SourceContentHash = oldScriptHash; // Lưu hash cũ để so sánh
        }

        // Mark các translation khác cần review nếu Vi thay đổi
        if (scriptChanged)
        {
            foreach (var trans in revision.Translations.Where(t => t.Language != "vi"))
            {
                trans.NeedsReview = true;
                trans.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);
        return UpdateContentResult.Ok;
    }

    /// <summary>
    /// Xóa phiên bản nháp
    /// </summary>
    public async Task<DeleteRevisionResult> DeleteDraftAsync(Guid revisionId, CancellationToken ct)
    {
        var revision = await _db.NarrationRevisions
            .Include(r => r.Jobs)
            .FirstOrDefaultAsync(r => r.Id == revisionId, ct);

        if (revision == null)
            return DeleteRevisionResult.NotFound;

        if (revision.Status != RevisionStatus.Draft)
            return DeleteRevisionResult.NotDraft;

        // Chặn xóa nếu có job đang Processing
        if (revision.Jobs.Any(j => j.Status == JobStatus.Processing))
            return DeleteRevisionResult.HasProcessingJobs;

        _db.NarrationRevisions.Remove(revision);
        await _db.SaveChangesAsync(ct);
        return DeleteRevisionResult.Ok;
    }

    /// <summary>
    /// Kiểm tra xem revision có job đang processing không
    /// </summary>
    public async Task<bool> HasProcessingJobsAsync(Guid revisionId, CancellationToken ct)
        => await _db.GenerationJobs.AnyAsync(
            j => j.RevisionId == revisionId && j.Status == JobStatus.Processing,
            ct);

    /// <summary>
    /// Admin duyệt bản dịch: giữ bản cũ hoặc cập nhật sang script Vi mới
    /// </summary>
    public async Task<UpdateContentResult> ReviewTranslationAsync(
        Guid revisionId,
        string language,
        ReviewTranslationRequest request,
        CancellationToken ct)
    {
        var revision = await _db.NarrationRevisions
            .Include(r => r.Translations)
            .Include(r => r.Jobs)
            .FirstOrDefaultAsync(r => r.Id == revisionId, ct);

        if (revision == null)
            return UpdateContentResult.NotFound;

        if (revision.Status != RevisionStatus.Draft)
            return UpdateContentResult.NotDraft;

        if (revision.Jobs.Any(j => j.Status == JobStatus.Processing))
            return UpdateContentResult.HasProcessingJobs;

        var translation = revision.Translations.FirstOrDefault(t => t.Language == language);
        if (translation == null)
            return UpdateContentResult.NotFound;

        if (!translation.NeedsReview)
            return UpdateContentResult.Ok; // Already reviewed or not marked for review

        if (request.Action == "UpdateToNew")
        {
            // Admin chọn cập nhật → regenerate từ Vi mới
            var viTrans = revision.Translations.FirstOrDefault(t => t.Language == "vi");
            if (viTrans != null)
            {
                // Reset translation để buộc regenerate
                translation.ScriptText = "";
                translation.IsEditedByAdmin = false;
                PublicationRules.Invalidate(translation);
                
                // Reset job để chạy lại
                var job = await _db.GenerationJobs.FirstOrDefaultAsync(
                    j => j.RevisionId == revisionId && j.Language == language, ct);
                if (job != null)
                {
                    job.Status = JobStatus.Queued;
                    job.Stage = JobStage.Translate;
                    job.LastError = null;
                }
            }
        }
        // else: "KeepOld" → giữ nguyên translation, chỉ mark as reviewed

        translation.NeedsReview = false;
        translation.UpdatedAt = DateTime.UtcNow;
        
        await _db.SaveChangesAsync(ct);
        return UpdateContentResult.Ok;
    }
}

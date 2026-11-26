using Microsoft.EntityFrameworkCore;
using Zoo.BUS.DTOs;
using Zoo.DAL.Data;
using Zoo.DAL.Entities;
using Zoo.DAL.Enums;
using Zoo.DAL.Interfaces;

namespace Zoo.BUS.Services;

/// <summary>
/// Service xuất bản phiên bản narration với kiểm tra & lưu trữ bản cũ
/// </summary>
public class PublicationService
{
    private static readonly string[] AllLangs = ["vi", "en", "ko", "zh", "ja"];
    
    private readonly ZooDbContext _db;
    private readonly IMediaStorage _storage;

    public PublicationService(ZooDbContext db, IMediaStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    /// <summary>
    /// Kiểm tra điều kiện xuất bản trước khi yêu cầu xác nhận
    /// </summary>
    public async Task<PublishRevisionResult> ValidatePublishRequirementsAsync(Guid revisionId, CancellationToken ct)
    {
        var revision = await _db.NarrationRevisions
            .Include(r => r.Translations)
            .Include(r => r.Jobs)
            .FirstOrDefaultAsync(r => r.Id == revisionId, ct);

        if (revision == null)
            return new PublishRevisionResult
            {
                Success = false,
                Message = "Không tìm thấy phiên bản.",
                MissingConditions = ["revision_not_found"]
            };

        var missing = new List<string>();

        // 1. Kiểm tra Status = Draft
        if (revision.Status != RevisionStatus.Draft)
        {
            missing.Add("not_draft_status");
        }

        // 2. Kiểm tra 5 ngôn ngữ có Title, Description, ScriptText, Audio hợp lệ
        var missingLanguages = new List<string>();
        foreach (var lang in AllLangs)
        {
            var trans = revision.Translations.FirstOrDefault(t => t.Language == lang);
            
            if (trans == null)
            {
                missingLanguages.Add(lang);
            }
            else if (string.IsNullOrWhiteSpace(trans.Title))
                missingLanguages.Add($"{lang}_missing_title");
            else if (string.IsNullOrWhiteSpace(trans.Description))
                missingLanguages.Add($"{lang}_missing_description");
            else if (string.IsNullOrWhiteSpace(trans.ScriptText))
                missingLanguages.Add($"{lang}_missing_script");
            else if (trans.AudioStatus != AudioStatus.Ready || string.IsNullOrWhiteSpace(trans.AudioPath))
                missingLanguages.Add($"{lang}_audio_not_ready");
            else if (trans.AudioContentHash != PublicationRules.Hash(trans.ScriptText))
                missingLanguages.Add($"{lang}_audio_hash_mismatch");
        }
        if (missingLanguages.Count > 0)
            missing.Add($"languages_not_ready: {string.Join(", ", missingLanguages)}");

        // 3. Kiểm tra tất cả job đã Succeeded
        var pendingJobs = revision.Jobs.Where(j => j.Status != JobStatus.Succeeded).ToList();
        if (pendingJobs.Count > 0)
        {
            missing.Add($"jobs_not_completed: {string.Join(", ", pendingJobs.Select(j => $"{j.Language}/{j.Status}"))}");
        }

        // 4. Kiểm tra tất cả translation không có NeedsReview=true
        var needsReview = revision.Translations.Where(t => t.NeedsReview).ToList();
        if (needsReview.Count > 0)
        {
            missing.Add($"translations_need_review: {string.Join(", ", needsReview.Select(t => t.Language))}");
        }

        // 5. Kiểm tra file audio tồn tại
        var missingAudioFiles = new List<string>();
        foreach (var trans in revision.Translations)
        {
            if (string.IsNullOrWhiteSpace(trans.AudioPath))
            {
                missingAudioFiles.Add(trans.Language);
            }
            else
            {
                try
                {
                    await using var audio = await _storage.OpenReadAudioAsync(trans.AudioPath, ct);
                    if (audio == null || audio.Length == 0)
                        missingAudioFiles.Add(trans.Language);
                }
                catch
                {
                    missingAudioFiles.Add(trans.Language);
                }
            }
        }
        if (missingAudioFiles.Count > 0)
            missing.Add($"audio_files_missing: {string.Join(", ", missingAudioFiles)}");

        if (missing.Count > 0)
        {
            return new PublishRevisionResult
            {
                Success = false,
                Message = "Điều kiện xuất bản chưa đủ. Vui lòng kiểm tra các yêu cầu dưới đây.",
                MissingConditions = missing
            };
        }

        return new PublishRevisionResult
        {
            Success = true,
            Message = "Tất cả điều kiện đã sẵn sàng. Bạn có thể xuất bản."
        };
    }

    /// <summary>
    /// Xuất bản phiên bản narration (có kiểm tra xác nhận + lưu trữ bản cũ trong giao dịch)
    /// </summary>
    public async Task<PublishRevisionResult> PublishAsync(Guid revisionId, PublishRevisionRequest request, CancellationToken ct)
    {
        // 1. Kiểm tra xác nhận
        var missingConfirmations = new List<string>();
        
        if (!request.ConfirmAllLanguagesReady)
            missingConfirmations.Add("missing_all_languages_confirm");
        if (!request.ConfirmAllAudioValid)
            missingConfirmations.Add("missing_audio_valid_confirm");
        if (!request.ConfirmAllJobsCompleted)
            missingConfirmations.Add("missing_jobs_confirm");
        if (!request.ConfirmAllTranslationsReviewed)
            missingConfirmations.Add("missing_review_confirm");

        if (missingConfirmations.Count > 0)
        {
            return new PublishRevisionResult
            {
                Success = false,
                Message = "Chưa xác nhận đầy đủ điều kiện xuất bản.",
                MissingConditions = missingConfirmations
            };
        }

        // 2. Load revision với tất cả quan hệ
        var revision = await _db.NarrationRevisions
            .Include(r => r.Place)
            .Include(r => r.Translations)
            .Include(r => r.Jobs)
            .FirstOrDefaultAsync(r => r.Id == revisionId, ct);

        if (revision == null)
            return new PublishRevisionResult
            {
                Success = false,
                Message = "Không tìm thấy phiên bản.",
                MissingConditions = ["revision_not_found"]
            };

        // 3. Revalidate điều kiện (tránh race condition)
        var validation = await ValidatePublishRequirementsAsync(revisionId, ct);
        if (!validation.Success)
            return validation;

        // 4. Bắt đầu giao dịch
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // 4a. Archive tất cả bản Published cũ
            var oldPublished = await _db.NarrationRevisions
                .Where(r => r.PlaceId == revision.PlaceId 
                    && r.Id != revision.Id 
                    && r.Status == RevisionStatus.Published)
                .ToListAsync(ct);

            foreach (var old in oldPublished)
            {
                old.Status = RevisionStatus.Archived;
                old.UpdatedAt = DateTime.UtcNow;
            }
            // Chỉ mục duy nhất theo điểm yêu cầu lưu trạng thái Archived trước.
            await _db.SaveChangesAsync(ct);

            // 4b. Set bản hiện tại thành Published
            revision.Status = RevisionStatus.Published;
            revision.PublishedAt = DateTime.UtcNow;
            revision.UpdatedAt = DateTime.UtcNow;
            // Trigger trên Places phải đọc được trạng thái Published đã lưu trong giao dịch.
            await _db.SaveChangesAsync(ct);

            // 4c. Trỏ PublishedRevisionId sang bản mới (SQL trigger sẽ validate)
            revision.Place.PublishedRevisionId = revision.Id;
            revision.Place.UpdatedAt = DateTime.UtcNow;

            // 4d. Lưu và commit giao dịch
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return new PublishRevisionResult
            {
                Success = true,
                Message = "Đã xuất bản thành công! Bản cũ được lưu trữ."
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            return new PublishRevisionResult
            {
                Success = false,
                Message = $"Lỗi xuất bản: {ex.Message}",
                MissingConditions = ["publish_failed"]
            };
        }
    }
}

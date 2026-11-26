using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Zoo.BUS.Services;
using Zoo.Api.Filters;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zoo.BUS.DTOs;
using Zoo.BUS.Interfaces;
using Zoo.DAL.Data;
using Zoo.DAL.Entities;
using Zoo.DAL.Enums;
using Zoo.DAL.Interfaces;

namespace Zoo.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
[AutoValidateAntiforgeryToken]
[ServiceFilter(typeof(ContentWriteFilter))]
public class AdminNarrationController : ControllerBase
{
    private readonly ZooDbContext _db;
    private readonly IMediaStorage _storage;
    private readonly NarrationEditingService _narrationEditing;
    private readonly GenerationService _generation;
    private readonly PublicationService _publication;

    private static readonly string[] AllLangs = ["vi", "en", "zh", "ko", "ja"];

    public AdminNarrationController(
        ZooDbContext db,
        IMediaStorage storage,
        NarrationEditingService narrationEditing,
        GenerationService generation,
        PublicationService publication)
    {
        _db = db;
        _storage = storage;
        _narrationEditing = narrationEditing;
        _generation = generation;
        _publication = publication;
    }

    // 2. Lấy danh sách phiên bản thuyết minh của 1 khu
    [HttpGet("places/{placeId:guid}/revisions")]
    public async Task<IActionResult> GetRevisions(Guid placeId, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";

        var revisions = await _db.NarrationRevisions
            .AsNoTracking()
            .Include(r => r.Translations)
            .Include(r => r.Jobs)
            .Where(r => r.PlaceId == placeId)
            .OrderByDescending(r => r.Version)
            .ToListAsync(ct);

        var res = revisions.Select(r => new
        {
            id = r.Id,
            placeId = r.PlaceId,
            version = r.Version,
            status = r.Status.ToString().ToLowerInvariant(),
            sourceText = r.Translations.FirstOrDefault(t => t.Language == "vi")?.ScriptText ?? "",
            createdAt = r.CreatedAt,
            translations = r.Translations.Select(t => new
            {
                lang = t.Language,
                text = t.ScriptText,
                needsReview = t.NeedsReview,
                isApproved = !t.NeedsReview && PublicationRules.Ready(t)
            }),
            jobs = r.Jobs.Select(j =>
            {
                var tr = r.Translations.FirstOrDefault(t => t.Language == j.Language);
                return new
                {
                    id = j.Id,
                    lang = j.Language,
                    status = j.Status.ToString().ToLowerInvariant(),
                    stage = j.Stage.ToString().ToLowerInvariant(),
                    errorMessage = j.LastError,
                    audioUrl = tr != null && PublicationRules.Ready(tr)
                        ? $"/api/admin/revisions/{r.Id}/audio/{tr.Language}?hash={tr.AudioContentHash}"
                        : null,
                    createdAt = j.CreatedAt,
                    updatedAt = j.UpdatedAt
                };
            })
        });

        return Ok(res);
    }

    public record CreateRevisionRequest(string SourceText);

    // 3. Tạo phiên bản nháp mới (Kế thừa Title & Description từ phiên bản trước)
    [HttpPost("places/{placeId:guid}/revisions")]
    public async Task<IActionResult> CreateRevision(Guid placeId, [FromBody] CreateRevisionRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.SourceText) || req.SourceText.Length > 20000)
            return BadRequest(new { message = "Bài thuyết minh phải có từ 1 đến 20.000 ký tự." });
        var place = await _db.Places
            .Include(p => p.Revisions)
                .ThenInclude(r => r.Translations)
            .FirstOrDefaultAsync(p => p.Id == placeId, ct);

        if (place == null) return NotFound(new { message = "Không tìm thấy khu tham quan." });

        var latestRev = place.Revisions.OrderByDescending(r => r.Version).FirstOrDefault();
        var prevVi = latestRev?.Translations.FirstOrDefault(t => t.Language == "vi");

        int nextVersion = (latestRev?.Version ?? 0) + 1;

        var rev = new NarrationRevision
        {
            PlaceId = placeId,
            Version = nextVersion,
            Status = RevisionStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            CreatedByAdminId = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var adminId) ? adminId : null
        };

        var viTitle = !string.IsNullOrWhiteSpace(prevVi?.Title) ? prevVi.Title : place.Code;
        var viDesc = !string.IsNullOrWhiteSpace(prevVi?.Description) ? prevVi.Description : req.SourceText;

        var viTranslation = new NarrationTranslation
        {
            RevisionId = rev.Id,
            Language = "vi",
            Title = viTitle,
            Description = viDesc,
            ScriptText = req.SourceText.Trim(),
            ContentHash = ComputeHash(req.SourceText.Trim()),
            UpdatedAt = DateTime.UtcNow
        };

        rev.Translations.Add(viTranslation);
        await _db.NarrationRevisions.AddAsync(rev, ct);
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            id = rev.Id,
            placeId = rev.PlaceId,
            version = rev.Version,
            status = "draft",
            sourceText = viTranslation.ScriptText,
            translations = new[] { new { lang = "vi", text = viTranslation.ScriptText, isApproved = false } },
            jobs = Array.Empty<object>(),
            createdAt = rev.CreatedAt
        });
    }

    public record UpdateTranslationRequest(string Text);

    // 4. Admin chỉnh sửa thủ công bản dịch của từng ngôn ngữ
    [HttpPut("revisions/{revisionId:guid}/translations/{lang}")]
    public async Task<IActionResult> UpdateTranslation(Guid revisionId, string lang, [FromBody] UpdateTranslationRequest req, CancellationToken ct)
    {
        var targetLang = lang.ToLowerInvariant().Trim();
        if (!AllLangs.Contains(targetLang) || string.IsNullOrWhiteSpace(req.Text) || req.Text.Length > 20000)
            return BadRequest(new { message = "Ngôn ngữ hoặc nội dung không hợp lệ (tối đa 20.000 ký tự)." });
        var (result, translation) = await _narrationEditing.UpdateTranslationAsync(revisionId, targetLang, req.Text, ct);
        return result switch
        {
            UpdateContentResult.NotFound => NotFound(new { message = "Không tìm thấy phiên bản." }),
            UpdateContentResult.NotDraft => Conflict(new { message = "Chỉ sửa nội dung trên bản nháp." }),
            UpdateContentResult.HasProcessingJobs => Conflict(new { message = "Không thể sửa nội dung khi có tác vụ đang xử lý." }),
            UpdateContentResult.Ok when translation != null => Ok(new
            {
                lang = targetLang,
                text = translation.ScriptText,
                needsReview = translation.NeedsReview,
                isApproved = !translation.NeedsReview && PublicationRules.Ready(translation)
            }),
            _ => StatusCode(500)
        };
    }

    // 4b. Duyệt lại bản dịch khi script tiếng Việt thay đổi (Admin chọn giữ cũ hoặc cập nhật sang mới)
    [HttpPost("revisions/{revisionId:guid}/translations/{lang}/review")]
    public async Task<IActionResult> ReviewTranslation(Guid revisionId, string lang, [FromBody] ReviewTranslationRequest req, CancellationToken ct)
    {
        var targetLang = lang.ToLowerInvariant().Trim();
        if (!AllLangs.Contains(targetLang) || targetLang == "vi" || req.Action is not ("KeepOld" or "UpdateToNew"))
            return BadRequest(new { message = "Ngôn ngữ hoặc thao tác duyệt không hợp lệ." });

        var result = await _narrationEditing.ReviewTranslationAsync(revisionId, targetLang, req, ct);
        
        return result switch
        {
            UpdateContentResult.Ok => Ok(new { message = $"Đã duyệt bản dịch {targetLang}." }),
            UpdateContentResult.NotFound => NotFound(new { message = "Không tìm thấy phiên bản hoặc bản dịch." }),
            UpdateContentResult.NotDraft => Conflict(new { message = "Chỉ duyệt bản nháp." }),
            UpdateContentResult.HasProcessingJobs => Conflict(new { message = "Không thể duyệt khi có job đang xử lý." }),
            _ => StatusCode(500, new { message = "Lỗi không xác định." })
        };
    }

    // 4c. Cập nhật Vi content (title, description, script_text) với detection thay đổi để mark review
    [HttpPut("revisions/{revisionId:guid}/content/vi")]
    public async Task<IActionResult> UpdateViContent(Guid revisionId, [FromBody] NarrationContentRequest req, CancellationToken ct)
    {
        if (req.ScriptText != null && (string.IsNullOrWhiteSpace(req.ScriptText) || req.ScriptText.Length > 20000))
            return BadRequest(new { message = "Bài thuyết minh phải có từ 1 đến 20.000 ký tự." });
        var result = await _narrationEditing.UpdateViContentWithReviewAsync(revisionId, req, ct);
        
        return result switch
        {
            UpdateContentResult.Ok => Ok(new { message = "Đã cập nhật nội dung tiếng Việt." }),
            UpdateContentResult.NotFound => NotFound(new { message = "Không tìm thấy phiên bản." }),
            UpdateContentResult.NotDraft => Conflict(new { message = "Chỉ cập nhật bản nháp." }),
            UpdateContentResult.HasProcessingJobs => Conflict(new { message = "Không thể cập nhật khi có job đang xử lý." }),
            _ => StatusCode(500, new { message = "Lỗi không xác định." })
        };
    }

    // 5. Dịch và tạo audio cho các ngôn ngữ của một bản nháp.
    [HttpPost("revisions/{revisionId:guid}/generate")]
    public Task<IActionResult> Generate(Guid revisionId, CancellationToken ct) =>
        GenerateRevisionAsync(revisionId, null, ct);

    private async Task<IActionResult> GenerateRevisionAsync(Guid revisionId, GenerationJob? selectedJob, CancellationToken ct)
    {
        var revision = await _db.NarrationRevisions.Include(r => r.Translations).Include(r => r.Jobs)
            .FirstOrDefaultAsync(r => r.Id == revisionId, ct);
        if (revision == null) return NotFound(new { message = "Không tìm thấy phiên bản." });
        if (revision.Status != RevisionStatus.Draft)
            return Conflict(new { message = "Chỉ tạo âm thanh cho bản nháp." });

        var source = revision.Translations.FirstOrDefault(t => t.Language == "vi");
        if (source == null || string.IsNullOrWhiteSpace(source.ScriptText))
            return BadRequest(new { message = "Thiếu nội dung thuyết minh tiếng Việt gốc." });

        await _generation.GenerateAsync(revision, source, selectedJob, ct);
        return await GetRevisions(revision.PlaceId, ct);
    }

    [HttpGet("revisions/{revisionId:guid}/jobs")]
    public async Task<IActionResult> Jobs(Guid revisionId, CancellationToken ct)
    {
        var jobs = await _db.GenerationJobs.AsNoTracking().Where(j => j.RevisionId == revisionId).ToListAsync(ct);
        return Ok(jobs.Select(j => new { id = j.Id, lang = j.Language,
            status = j.Status.ToString().ToLowerInvariant(), stage = j.Stage.ToString().ToLowerInvariant(),
            errorMessage = j.LastError, audioUrl = (string?)null, createdAt = j.CreatedAt, updatedAt = j.UpdatedAt }));
    }

    // 6. Retry 1 job bị lỗi
    [HttpPost("jobs/{jobId:guid}/retry")]
    public async Task<IActionResult> RetryJob(Guid jobId, CancellationToken ct)
    {
        var job = await _db.GenerationJobs.FindAsync([jobId], ct);
        if (job == null) return NotFound();
        if (job.Status != JobStatus.Failed)
            return Conflict(new { message = "Chỉ thử lại tác vụ đã lỗi." });
        return await GenerateRevisionAsync(job.RevisionId, job, ct);
    }

    // UC06.3 — Xóa bản nháp (chỉ Draft, không xóa Published/Archived — BR-25)
    [HttpDelete("revisions/{revisionId:guid}")]
    public async Task<IActionResult> DeleteRevision(Guid revisionId, CancellationToken ct)
    {
        var revision = await _db.NarrationRevisions
            .Include(r => r.Jobs)
            .FirstOrDefaultAsync(r => r.Id == revisionId, ct);

        if (revision == null) return NotFound(new { message = "Không tìm thấy phiên bản." });

        if (revision.Status != RevisionStatus.Draft)
            return Conflict(new { message = "Chỉ có thể xóa bản nháp (Draft). Phiên bản đã xuất bản hoặc lưu trữ không thể xóa." });

        // BR-27: không xóa khi có job đang xử lý
        bool hasRunning = revision.Jobs.Any(j => j.Status == JobStatus.Processing);
        if (hasRunning)
            return Conflict(new { message = "Đang có tác vụ xử lý. Vui lòng chờ hoàn tất rồi thử lại." });

        _db.NarrationRevisions.Remove(revision);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // 7a. Validate publish requirements
    [HttpGet("revisions/{revisionId:guid}/publish/validate")]
    public async Task<IActionResult> ValidatePublish(Guid revisionId, CancellationToken ct)
    {
        var result = await _publication.ValidatePublishRequirementsAsync(revisionId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // 7b. Xuất bản phiên bản (yêu cầu xác nhận + lưu trữ bản cũ)
    [HttpPost("revisions/{revisionId:guid}/publish")]
    public async Task<IActionResult> Publish(Guid revisionId, [FromBody] PublishRevisionRequest request, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";

        if (request == null)
            return BadRequest(new { message = "Yêu cầu không hợp lệ." });

        var result = await _publication.PublishAsync(revisionId, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // 7c. (Deprecated) Legacy publish endpoint - sử dụng POST /api/admin/revisions/{revisionId}/publish thay vào
    [HttpPut("revisions/{revisionId:guid}/publish-legacy")]
    [Obsolete("Use POST /api/admin/revisions/{revisionId}/publish instead")]
    public async Task<IActionResult> PublishLegacy(Guid revisionId, CancellationToken ct)
    {
        var revision = await _db.NarrationRevisions
            .Include(r => r.Place)
            .FirstOrDefaultAsync(r => r.Id == revisionId, ct);

        if (revision == null) return NotFound(new { message = "Không tìm thấy phiên bản." });
        if (revision.Status != RevisionStatus.Draft)
            return Conflict(new { message = "Chỉ sửa, tạo âm thanh hoặc xuất bản bản nháp. Hãy tạo phiên bản mới." });

        await _db.Entry(revision).Collection(r => r.Translations).LoadAsync(ct);
        var missing = AllLangs.Where(l => !revision.Translations.Any(t => t.Language == l && PublicationRules.Ready(t))).ToArray();
        if (missing.Length > 0)
            return BadRequest(new { message = "Chưa sẵn sàng xuất bản: " + string.Join(", ", missing) });
        foreach (var tr in revision.Translations)
        {
            await using var audio = await _storage.OpenReadAudioAsync(tr.AudioPath!, ct);
            if (audio == null || audio.Length == 0)
                return BadRequest(new { message = $"Không tìm thấy file âm thanh {tr.Language}. Hãy tạo lại âm thanh." });
        }
        // Đưa các bản Published cũ của khu này về Archived
        var oldPublished = await _db.NarrationRevisions
            .Where(r => r.PlaceId == revision.PlaceId && r.Id != revision.Id && r.Status == RevisionStatus.Published)
            .ToListAsync(ct);

        foreach (var old in oldPublished)
        {
            old.Status = RevisionStatus.Archived;
        }

        revision.Status = RevisionStatus.Published;
        revision.PublishedAt = DateTime.UtcNow;

        // Trỏ khóa ngoại PublishedRevisionId sang phiên bản vừa xuất bản
        revision.Place.PublishedRevisionId = revision.Id;
        revision.Place.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(new { message = "Đã xuất bản thành công!" });
    }



    private static string ComputeHash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text ?? ""));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

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
    private readonly IPlaceService _placeService;
    private readonly ITranslationClient _translator;
    private readonly ISpeechClient _speechClient;
    private readonly IMediaStorage _storage;

    private static readonly string[] AllLangs = ["vi", "en", "zh", "ko", "ja"];

    public AdminNarrationController(
        ZooDbContext db,
        IPlaceService placeService,
        ITranslationClient translator,
        ISpeechClient speechClient,
        IMediaStorage storage)
    {
        _db = db;
        _placeService = placeService;
        _translator = translator;
        _speechClient = speechClient;
        _storage = storage;
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
                isApproved = PublicationRules.Ready(t)
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
        var revision = await _db.NarrationRevisions
            .Include(r => r.Translations)
            .Include(r => r.Place)
            .FirstOrDefaultAsync(r => r.Id == revisionId, ct);

        if (revision == null) return NotFound(new { message = "Không tìm thấy phiên bản." });
        if (revision.Status != RevisionStatus.Draft)
            return Conflict(new { message = "Chỉ sửa, tạo âm thanh hoặc xuất bản bản nháp. Hãy tạo phiên bản mới." });

        var viTrans = revision.Translations.FirstOrDefault(t => t.Language == "vi");
        var translation = revision.Translations.FirstOrDefault(t => t.Language == targetLang);

        if (translation == null)
        {
            translation = new NarrationTranslation
            {
                RevisionId = revisionId,
                Language = targetLang,
                Title = targetLang == "vi" ? viTrans?.Title ?? revision.Place.Code : "",
                Description = "",
                UpdatedAt = DateTime.UtcNow
            };
            _db.NarrationTranslations.Add(translation);
        }

        translation.ScriptText = req.Text.Trim();
        translation.IsEditedByAdmin = true;
        translation.ContentHash = ComputeHash(translation.ScriptText);
        PublicationRules.Invalidate(translation);
        if (targetLang == "vi")
            foreach (var other in revision.Translations.Where(t => t.Language != "vi"))
            {
                other.ScriptText = ""; other.IsEditedByAdmin = false;
                PublicationRules.Invalidate(other);
            }
        var staleJobs = await _db.GenerationJobs.Where(j => j.RevisionId == revisionId
            && (targetLang == "vi" || j.Language == targetLang)).ToListAsync(ct);
        foreach (var job in staleJobs) { job.Status = JobStatus.Queued; job.LastError = null; }
        translation.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(new { lang = targetLang, text = translation.ScriptText, isApproved = false });
    }

    // 5. Dịch tự động qua Azure Translator & Tạo file MP3 qua Azure Speech
    [HttpPost("revisions/{revisionId:guid}/generate")]
    public async Task<IActionResult> Generate(Guid revisionId, CancellationToken ct)
    {
        var revision = await _db.NarrationRevisions
            .Include(r => r.Translations)
            .Include(r => r.Jobs)
            .Include(r => r.Place)
            .FirstOrDefaultAsync(r => r.Id == revisionId, ct);

        if (revision == null) return NotFound(new { message = "Không tìm thấy phiên bản." });
        if (revision.Status != RevisionStatus.Draft)
            return Conflict(new { message = "Chỉ sửa, tạo âm thanh hoặc xuất bản bản nháp. Hãy tạo phiên bản mới." });

        var viTrans = revision.Translations.FirstOrDefault(t => t.Language == "vi");
        if (viTrans == null || string.IsNullOrWhiteSpace(viTrans.ScriptText))
        {
            return BadRequest(new { message = "Thiếu nội dung thuyết minh tiếng Việt gốc." });
        }

        foreach (var l in AllLangs)
        {
            var trans = revision.Translations.FirstOrDefault(t => t.Language == l);
            if (trans == null)
            {
                trans = new NarrationTranslation
                {
                    RevisionId = revision.Id,
                    Language = l,
                    Title = viTrans.Title,
                    Description = viTrans.Description,
                    UpdatedAt = DateTime.UtcNow
                };
                revision.Translations.Add(trans);
                _db.NarrationTranslations.Add(trans);
            }

            var job = revision.Jobs.FirstOrDefault(j => j.Language == l);
            if (job == null)
            {
                job = new GenerationJob
                {
                    RevisionId = revision.Id,
                    Language = l,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                revision.Jobs.Add(job);
                _db.GenerationJobs.Add(job);
            }

            // BR-27: reset job bị kẹt từ lần chạy trước (Processing → Queued)
            if (job.Status == JobStatus.Processing)
            {
                job.Status = JobStatus.Queued;
                job.LastError = null;
            }

            try
            {
                // Bước A: Dịch tự động cho en, zh, ko, ja
                if (l != "vi" && (string.IsNullOrWhiteSpace(trans.ScriptText) || string.IsNullOrWhiteSpace(trans.Title)))
                {
                    job.Stage = JobStage.Translate;
                    job.Status = JobStatus.Processing;

                    var title = await _translator.TranslateAsync(viTrans.Title, "vi", l, ct);
                    var script = trans.IsEditedByAdmin && !string.IsNullOrWhiteSpace(trans.ScriptText)
                        ? trans.ScriptText : await _translator.TranslateAsync(viTrans.ScriptText, "vi", l, ct);
                    var description = string.IsNullOrWhiteSpace(viTrans.Description) ? "" : await _translator.TranslateAsync(viTrans.Description, "vi", l, ct);
                    trans.Title = title; trans.ScriptText = script; trans.Description = description;
                    trans.ContentHash = ComputeHash(trans.ScriptText);
                }

                // Bước B: Tạo file MP3 bằng Azure Speech
                var textHash = ComputeHash(trans.ScriptText);
                bool fileExists = false;
                if (!string.IsNullOrWhiteSpace(trans.AudioPath))
                {
                    await using var existing = await _storage.OpenReadAudioAsync(trans.AudioPath, ct);
                    fileExists = existing != null && existing.Length > 0;
                }
                if (!PublicationRules.Ready(trans) || !fileExists)
                {
                    job.Status = JobStatus.Processing;
                    job.Stage = JobStage.Synthesize;
                    var audioBytes = await _speechClient.SynthesizeAsync(trans.ScriptText, l, ct);

                    if (audioBytes.Length == 0) throw new InvalidOperationException("Azure trả về file rỗng.");
                    job.Stage = JobStage.Store;
                    var relativePath = await _storage.SaveAudioAsync(revision.PlaceId, revision.Id, l, audioBytes, ct);

                    trans.AudioPath = relativePath;
                    trans.AudioContentHash = textHash;
                    trans.AudioStatus = AudioStatus.Ready;
                }

                trans.UpdatedAt = DateTime.UtcNow;
                job.Status = JobStatus.Succeeded;
                job.LastError = null;
                job.UpdatedAt = DateTime.UtcNow;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                trans.AudioStatus = AudioStatus.Failed;
                job.Status = JobStatus.Failed;
                job.LastError = GenerationError(ex);
                HttpContext.RequestServices.GetRequiredService<ILogger<AdminNarrationController>>().LogError(ex, "Generation failed for {RevisionId}/{Language}", revisionId, l);
                job.UpdatedAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync(ct);
        }

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
        return await Generate(job.RevisionId, ct);
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

    // 7. Xuất bản phiên bản và cập nhật phiên bản công khai của điểm.
    [HttpPost("revisions/{revisionId:guid}/publish")]
    public async Task<IActionResult> Publish(Guid revisionId, CancellationToken ct)
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

    private static string GenerationError(Exception ex)
    {
        // Return only known configuration messages/status codes; do not expose provider response bodies.
        if (ex.Message == "Chưa cấu hình Azure Speech ApiKey." || ex.Message == "Chưa cấu hình Azure Translator ApiKey.")
            return ex.Message + " Cấu hình key bằng User Secrets rồi khởi động lại backend.";
        var match = System.Text.RegularExpressions.Regex.Match(ex.Message, @"^Lỗi Azure (Speech|Translator) \((\d{3})\)");
        if (match.Success)
        {
            var service = match.Groups[1].Value;
            var status = match.Groups[2].Value;
            var action = status switch
            {
                "401" => "Kiểm tra key và region có thuộc đúng tài nguyên Azure đang gọi.",
                "403" => "Kiểm tra quyền truy cập, trạng thái tài nguyên, mạng và hạn mức Azure.",
                "429" => "Đang bị giới hạn yêu cầu. Kiểm tra hạn mức Azure và thử lại sau.",
                "400" => "Azure từ chối nội dung hoặc cấu hình yêu cầu. Xem log backend để biết chi tiết.",
                _ => "Kiểm tra dịch vụ Azure và log backend rồi thử lại."
            };
            return $"Azure {service} HTTP {status}. {action}";
        }
        if (ex is OperationCanceledException) return "Dịch vụ Azure phản hồi quá thời gian chờ. Hãy thử lại.";
        if (ex is HttpRequestException) return "Không kết nối được Azure. Kiểm tra mạng, endpoint và region; xem log backend.";
        if (ex is IOException || ex is UnauthorizedAccessException) return "Không đọc/ghi được file audio. Kiểm tra thư mục storage và quyền ghi; xem log backend.";
        return "Không tạo được âm thanh. Xem log backend tại dòng Generation failed để biết nguyên nhân.";
    }

    private static string ComputeHash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text ?? ""));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
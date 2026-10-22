using System.Security.Cryptography;
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

    // 1. Lấy toàn bộ danh sách khu cho Admin
    [HttpGet("places")]
    public async Task<ActionResult<IReadOnlyList<PlaceDto>>> GetAdminPlaces(CancellationToken ct)
    {
        var places = await _placeService.GetAllForAdminAsync(ct);
        return Ok(places);
    }

    // 2. Lấy danh sách phiên bản thuyết minh của 1 khu
    [HttpGet("places/{placeId:guid}/revisions")]
    public async Task<IActionResult> GetRevisions(Guid placeId, CancellationToken ct)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

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
                isApproved = t.AudioStatus == AudioStatus.Ready
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
                    audioUrl = !string.IsNullOrEmpty(tr?.AudioPath)
                        ? $"{baseUrl}/storage/{tr.AudioPath.TrimStart('/')}"
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
            CreatedAt = DateTime.UtcNow
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
        var revision = await _db.NarrationRevisions
            .Include(r => r.Translations)
            .Include(r => r.Place)
            .FirstOrDefaultAsync(r => r.Id == revisionId, ct);

        if (revision == null) return NotFound(new { message = "Không tìm thấy phiên bản." });

        var viTrans = revision.Translations.FirstOrDefault(t => t.Language == "vi");
        var translation = revision.Translations.FirstOrDefault(t => t.Language == targetLang);

        if (translation == null)
        {
            translation = new NarrationTranslation
            {
                RevisionId = revisionId,
                Language = targetLang,
                Title = viTrans?.Title ?? revision.Place.Code,
                Description = req.Text,
                UpdatedAt = DateTime.UtcNow
            };
            _db.NarrationTranslations.Add(translation);
        }

        translation.ScriptText = req.Text.Trim();
        translation.IsEditedByAdmin = true;
        translation.ContentHash = ComputeHash(translation.ScriptText);
        translation.AudioStatus = AudioStatus.Missing;
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
                    CreatedAt = DateTime.UtcNow
                };
                revision.Jobs.Add(job);
                _db.GenerationJobs.Add(job);
            }

            try
            {
                // Bước A: Dịch tự động cho en, zh, ko, ja
                if (l != "vi" && (!trans.IsEditedByAdmin || string.IsNullOrWhiteSpace(trans.ScriptText)))
                {
                    job.Stage = JobStage.Translate;
                    job.Status = JobStatus.Processing;

                    trans.Title = await _translator.TranslateAsync(viTrans.Title, "vi", l, ct);
                    trans.ScriptText = await _translator.TranslateAsync(viTrans.ScriptText, "vi", l, ct);
                    trans.Description = trans.ScriptText;
                    trans.ContentHash = ComputeHash(trans.ScriptText);
                }

                // Bước B: Tạo file MP3 bằng Azure Speech
                var textHash = ComputeHash(trans.ScriptText);
                if (trans.AudioStatus != AudioStatus.Ready || trans.AudioContentHash != textHash || string.IsNullOrEmpty(trans.AudioPath))
                {
                    job.Stage = JobStage.Synthesize;
                    var audioBytes = await _speechClient.SynthesizeAsync(trans.ScriptText, l, ct);

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
            catch (Exception ex)
            {
                trans.AudioStatus = AudioStatus.Failed;
                job.Status = JobStatus.Failed;
                job.LastError = ex.Message;
                job.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);
        return await GetRevisions(revision.PlaceId, ct);
    }

    // 6. Retry 1 job bị lỗi
    [HttpPost("jobs/{jobId:guid}/retry")]
    public async Task<IActionResult> RetryJob(Guid jobId, CancellationToken ct)
    {
        var job = await _db.GenerationJobs.FindAsync([jobId], ct);
        if (job == null) return NotFound();
        return await Generate(job.RevisionId, ct);
    }

    // 7. Xuất bản phiên bản -> Cập nhật thẳng vào Place.PublishedRevisionId cho Web User
    [HttpPost("revisions/{revisionId:guid}/publish")]
    public async Task<IActionResult> Publish(Guid revisionId, CancellationToken ct)
    {
        var revision = await _db.NarrationRevisions
            .Include(r => r.Place)
            .FirstOrDefaultAsync(r => r.Id == revisionId, ct);

        if (revision == null) return NotFound(new { message = "Không tìm thấy phiên bản." });

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
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

    private static readonly string[] TargetLangs = ["en", "zh", "ko", "ja"];

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

    // GET /api/admin/places (Dùng cho dropdown chọn khu trong Admin)
    [HttpGet("places")]
    public async Task<ActionResult<IReadOnlyList<PlaceDto>>> GetAdminPlaces(CancellationToken ct)
    {
        return Ok(await _placeService.GetPublishedAsync("vi", ct));
    }

    // GET /api/admin/places/{placeId}/revisions
    [HttpGet("places/{placeId:guid}/revisions")]
    public async Task<IActionResult> GetRevisions(Guid placeId, CancellationToken ct)
    {
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
            jobs = r.Jobs.Select(j => new
            {
                id = j.Id,
                lang = j.Language,
                status = j.Status.ToString().ToLowerInvariant(),
                stage = j.Stage.ToString().ToLowerInvariant(),
                errorMessage = j.LastError,
                audioUrl = r.Translations.FirstOrDefault(t => t.Language == j.Language && t.AudioPath != null) != null
                    ? $"/storage/{r.Translations.First(t => t.Language == j.Language).AudioPath}"
                    : null
            })
        });

        return Ok(res);
    }

    public record CreateRevisionRequest(string SourceText);

    // POST /api/admin/places/{placeId}/revisions
    [HttpPost("places/{placeId:guid}/revisions")]
    public async Task<IActionResult> CreateRevision(Guid placeId, [FromBody] CreateRevisionRequest req, CancellationToken ct)
    {
        var place = await _db.Places.FindAsync(new object[] { placeId }, ct);
        if (place == null) return NotFound(new { message = "Không tìm thấy khu tham quan." });

        int latestVersion = await _db.NarrationRevisions
            .Where(r => r.PlaceId == placeId)
            .Select(r => (int?)r.Version).MaxAsync(ct) ?? 0;

        var rev = new NarrationRevision
        {
            PlaceId = placeId,
            Version = latestVersion + 1,
            Status = RevisionStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        var viTranslation = new NarrationTranslation
        {
            RevisionId = rev.Id,
            Language = "vi",
            Title = place.Code,
            Description = req.SourceText,
            ScriptText = req.SourceText,
            ContentHash = ComputeHash(req.SourceText),
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
            sourceText = req.SourceText,
            translations = new[] { new { lang = "vi", text = req.SourceText, isApproved = false } },
            jobs = Array.Empty<object>(),
            createdAt = rev.CreatedAt
        });
    }

    public record UpdateTranslationRequest(string Text);

    // PUT /api/admin/revisions/{revisionId}/translations/{lang}
    [HttpPut("revisions/{revisionId:guid}/translations/{lang}")]
    public async Task<IActionResult> UpdateTranslation(Guid revisionId, string lang, [FromBody] UpdateTranslationRequest req, CancellationToken ct)
    {
        var targetLang = lang.ToLowerInvariant().Trim();
        var translation = await _db.NarrationTranslations
            .FirstOrDefaultAsync(t => t.RevisionId == revisionId && t.Language == targetLang, ct);

        if (translation == null)
        {
            translation = new NarrationTranslation
            {
                RevisionId = revisionId,
                Language = targetLang,
                UpdatedAt = DateTime.UtcNow
            };
            _db.NarrationTranslations.Add(translation);
        }

        translation.ScriptText = req.Text;
        translation.IsEditedByAdmin = true;
        translation.ContentHash = ComputeHash(req.Text);
        translation.AudioStatus = AudioStatus.Missing; // Nội dung thay đổi -> cần sinh lại audio
        await _db.SaveChangesAsync(ct);

        return Ok(new { lang = targetLang, text = req.Text, isApproved = false });
    }

    // POST /api/admin/revisions/{revisionId}/generate (Dịch tự động và tạo âm thanh MP3)
    [HttpPost("revisions/{revisionId:guid}/generate")]
    public async Task<IActionResult> Generate(Guid revisionId, CancellationToken ct)
    {
        var revision = await _db.NarrationRevisions
            .Include(r => r.Translations)
            .Include(r => r.Jobs)
            .Include(r => r.Place)
            .FirstOrDefaultAsync(r => r.Id == revisionId, ct);

        if (revision == null) return NotFound("Không tìm thấy phiên bản này.");

        var viTrans = revision.Translations.FirstOrDefault(t => t.Language == "vi");
        if (viTrans == null || string.IsNullOrWhiteSpace(viTrans.ScriptText))
        {
            return BadRequest("Thiếu nội dung thuyết minh tiếng Việt gốc.");
        }

        var allLangs = new List<string> { "vi" };
        allLangs.AddRange(TargetLangs);

        foreach (var l in allLangs)
        {
            var trans = revision.Translations.FirstOrDefault(t => t.Language == l);
            if (trans == null)
            {
                trans = new NarrationTranslation
                {
                    RevisionId = revision.Id,
                    Language = l,
                    Title = revision.Place.Code,
                    UpdatedAt = DateTime.UtcNow
                };
                revision.Translations.Add(trans);
            }

            // Dịch nếu chưa phải tiếng Việt và chưa có nội dung admin sửa tay
            if (l != "vi" && (string.IsNullOrWhiteSpace(trans.ScriptText) || !trans.IsEditedByAdmin))
            {
                trans.ScriptText = await _translator.TranslateAsync(viTrans.ScriptText, "vi", l, ct);
                trans.Description = trans.ScriptText;
                trans.ContentHash = ComputeHash(trans.ScriptText);
            }

            // Sinh file Audio MP3 qua Azure AI Speech nếu chưa sẵn sàng
            if (trans.AudioStatus != AudioStatus.Ready || string.IsNullOrEmpty(trans.AudioPath))
            {
                try
                {
                    var audioBytes = await _speechClient.SynthesizeAsync(trans.ScriptText, l, ct);
                    var audioPath = await _storage.SaveAudioAsync(revision.PlaceId, revision.Id, l, audioBytes, ct);
                    trans.AudioPath = audioPath;
                    trans.AudioStatus = AudioStatus.Ready;
                    trans.AudioContentHash = ComputeHash(trans.ScriptText);
                }
                catch (Exception ex)
                {
                    trans.AudioStatus = AudioStatus.Failed;
                    var job = revision.Jobs.FirstOrDefault(j => j.Language == l) ?? new GenerationJob { RevisionId = revision.Id, Language = l };
                    job.Status = JobStatus.Failed;
                    job.LastError = ex.Message;
                    if (!_db.GenerationJobs.Contains(job)) _db.GenerationJobs.Add(job);
                }
            }

            // Tạo/cập nhật Job thành công
            var curJob = revision.Jobs.FirstOrDefault(j => j.Language == l);
            if (curJob == null)
            {
                curJob = new GenerationJob { RevisionId = revision.Id, Language = l };
                _db.GenerationJobs.Add(curJob);
            }
            curJob.Status = trans.AudioStatus == AudioStatus.Ready ? JobStatus.Succeeded : JobStatus.Failed;
            curJob.Stage = JobStage.Store;
            curJob.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { message = "Đã sinh dịch thuật và tệp âm thanh thành công." });
    }

    // POST /api/admin/revisions/{revisionId}/publish (XUẤT BẢN NGAY CHO WEB USER)
    [HttpPost("revisions/{revisionId:guid}/publish")]
    public async Task<IActionResult> Publish(Guid revisionId, CancellationToken ct)
    {
        var revision = await _db.NarrationRevisions
            .Include(r => r.Place)
            .FirstOrDefaultAsync(r => r.Id == revisionId, ct);

        if (revision == null) return NotFound("Không tìm thấy revision.");

        // Chuyển các revision cũ của địa điểm này sang Archived
        var oldRevs = await _db.NarrationRevisions
            .Where(r => r.PlaceId == revision.PlaceId && r.Id != revision.Id && r.Status == RevisionStatus.Published)
            .ToListAsync(ct);
        foreach (var old in oldRevs) old.Status = RevisionStatus.Archived;

        revision.Status = RevisionStatus.Published;
        revision.PublishedAt = DateTime.UtcNow;

        // CẬP NHẬT TRỰC TIẾP VÀO PLACE -> WEB USER SẼ THẤY NGAY LẬP TỨC!
        revision.Place.PublishedRevisionId = revision.Id;
        revision.Place.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(new { message = "Đã xuất bản thành công! Web User đã được cập nhật dữ liệu mới nhất." });
    }

    private static string ComputeHash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
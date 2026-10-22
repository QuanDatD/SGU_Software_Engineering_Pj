using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Zoo.BUS.DTOs;
using Zoo.BUS.Interfaces;
using Zoo.DAL.Data;
using Zoo.DAL.Entities;
using Zoo.DAL.Enums;
using Zoo.DAL.Interfaces;

namespace Zoo.BUS.Services;

public class NarrationService : INarrationService
{
    private static readonly string[] TargetLanguages = ["en", "zh", "ko", "ja"];

    private readonly ZooDbContext _db;
    private readonly ITranslationClient _translator;
    private readonly ISpeechClient _speechClient;
    private readonly IMediaStorage _storage;
    private readonly IUnitOfWork _unitOfWork;

    public NarrationService(
        ZooDbContext db,
        ITranslationClient translator,
        ISpeechClient speechClient,
        IMediaStorage storage,
        IUnitOfWork unitOfWork)
    {
        _db = db;
        _translator = translator;
        _speechClient = speechClient;
        _storage = storage;
        _unitOfWork = unitOfWork;
    }

    public async Task<NarrationDto?> GetNarrationAsync(Guid placeId, string language, CancellationToken ct = default)
    {
        var lang = language.ToLowerInvariant().Trim();

        // Tìm bản revision đã xuất bản
        var revision = await _db.NarrationRevisions
            .Include(r => r.Place)
            .Include(r => r.Translations)
            .Where(r => r.PlaceId == placeId && r.Status == RevisionStatus.Published)
            .OrderByDescending(r => r.Version)
            .FirstOrDefaultAsync(ct);

        if (revision == null) return null;

        // Ưu tiên tìm ngôn ngữ yêu cầu, nếu không có fallback về tiếng Việt
        var translation = revision.Translations.FirstOrDefault(t => t.Language.Equals(lang, StringComparison.OrdinalIgnoreCase))
                          ?? revision.Translations.FirstOrDefault(t => t.Language == "vi");

        if (translation == null) return null;

        return new NarrationDto
        {
            PlaceId = revision.PlaceId,
            PlaceCode = revision.Place.Code,
            Language = translation.Language,
            Title = translation.Title,
            Description = translation.Description,
            ScriptText = translation.ScriptText,
            AudioUrl = !string.IsNullOrEmpty(translation.AudioPath) ? $"/storage/{translation.AudioPath}" : null,
            HasAudio = translation.AudioStatus == AudioStatus.Ready && !string.IsNullOrEmpty(translation.AudioPath)
        };
    }

    public async Task<GenerationResultDto> GenerateNarrationContentAsync(Guid placeId, bool forceOverwrite = false, CancellationToken ct = default)
    {
        var place = await _db.Places.FindAsync([placeId], ct);
        if (place == null)
            throw new KeyNotFoundException($"Không tìm thấy địa điểm với Id: {placeId}");

        // Lấy revision mới nhất
        var revision = await _db.NarrationRevisions
            .Include(r => r.Translations)
            .Where(r => r.PlaceId == placeId)
            .OrderByDescending(r => r.Version)
            .FirstOrDefaultAsync(ct);

        if (revision == null)
        {
            revision = new NarrationRevision
            {
                PlaceId = placeId,
                Version = 1,
                Status = RevisionStatus.Published,
                PublishedAt = DateTime.UtcNow
            };
            await _db.NarrationRevisions.AddAsync(revision, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        // Tìm bản tiếng Việt làm bản gốc
        var viTranslation = revision.Translations.FirstOrDefault(t => t.Language == "vi");
        if (viTranslation == null || string.IsNullOrWhiteSpace(viTranslation.ScriptText))
        {
            throw new InvalidOperationException("Địa điểm này chưa có bài thuyết minh gốc bằng tiếng Việt (vi). Vui lòng thêm trước khi dịch.");
        }

        var result = new GenerationResultDto { PlaceId = placeId };

        // 1. TÍNH HASH BẢN GỐC TIẾNG VIỆT
        var currentViHash = ComputeSha256(viTranslation.Title + viTranslation.Description + viTranslation.ScriptText);
        viTranslation.ContentHash = currentViHash;

        // 2. SINH AUDIO CHO BẢN TIẾNG VIỆT (NẾU CHƯA CÓ HOẶC BỊ THAY ĐỔI)
        if (forceOverwrite || viTranslation.AudioStatus != AudioStatus.Ready || viTranslation.AudioContentHash != currentViHash)
        {
            await GenerateAudioAsync(place.Id, revision.Id, viTranslation, ct);
            result.SynthesizedAudioCount++;
        }

        // 3. XỬ LÝ DỊCH VÀ SINH AUDIO CHO CÁC NGÔN NGỮ ĐÍCH
        foreach (var targetLang in TargetLanguages)
        {
            var translation = revision.Translations.FirstOrDefault(t => t.Language.Equals(targetLang, StringComparison.OrdinalIgnoreCase));
            bool isNew = false;

            if (translation == null)
            {
                translation = new NarrationTranslation
                {
                    RevisionId = revision.Id,
                    Language = targetLang,
                    IsEditedByAdmin = false
                };
                revision.Translations.Add(translation);
                isNew = true;
            }

            // A. DỊCH VĂN BẢN (TRANSLATE)
            // Không ghi đè nếu admin đã chỉnh sửa tay (trừ khi forceOverwrite = true)
            if (isNew || (forceOverwrite || (!translation.IsEditedByAdmin && translation.ContentHash != currentViHash)))
            {
                translation.Title = await _translator.TranslateAsync(viTranslation.Title, "vi", targetLang, ct);
                translation.Description = await _translator.TranslateAsync(viTranslation.Description, "vi", targetLang, ct);
                translation.ScriptText = await _translator.TranslateAsync(viTranslation.ScriptText, "vi", targetLang, ct);
                translation.ContentHash = currentViHash;
                translation.UpdatedAt = DateTime.UtcNow;
                result.TranslatedCount++;
            }

            // B. TẠO FILE ÂM THANH (TTS)
            var targetTextHash = ComputeSha256(translation.ScriptText);
            if (forceOverwrite || translation.AudioStatus != AudioStatus.Ready || translation.AudioContentHash != targetTextHash)
            {
                await GenerateAudioAsync(place.Id, revision.Id, translation, ct);
                result.SynthesizedAudioCount++;
            }

            result.ProcessedLanguages.Add(targetLang);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        result.Message = $"Xử lý thành công: {result.TranslatedCount} bản dịch, {result.SynthesizedAudioCount} file MP3.";
        return result;
    }

    public async Task<List<GenerationResultDto>> GenerateAllPlacesAsync(bool forceOverwrite = false, CancellationToken ct = default)
    {
        var placeIds = await _db.Places.Where(p => p.IsActive).Select(p => p.Id).ToListAsync(ct);
        var results = new List<GenerationResultDto>();

        foreach (var pid in placeIds)
        {
            try
            {
                var res = await GenerateNarrationContentAsync(pid, forceOverwrite, ct);
                results.Add(res);
            }
            catch (Exception ex)
            {
                results.Add(new GenerationResultDto
                {
                    PlaceId = pid,
                    Message = $"Lỗi: {ex.Message}"
                });
            }
        }

        return results;
    }

    private async Task GenerateAudioAsync(Guid placeId, Guid revisionId, NarrationTranslation translation, CancellationToken ct)
    {
        try
        {
            translation.AudioStatus = AudioStatus.Generating;

            // Gọi Azure AI Speech tổng hợp file nhị phân MP3
            var audioBytes = await _speechClient.SynthesizeAsync(translation.ScriptText, translation.Language, ct);

            // Lưu file vào storage cục bộ
            var relativePath = await _storage.SaveAudioAsync(placeId, revisionId, translation.Language, audioBytes, ct);

            translation.AudioPath = relativePath;
            translation.AudioStatus = AudioStatus.Ready;
            translation.AudioContentHash = ComputeSha256(translation.ScriptText);
            translation.UpdatedAt = DateTime.UtcNow;
        }
        catch
        {
            translation.AudioStatus = AudioStatus.Failed;
            throw;
        }
    }

    private static string ComputeSha256(string rawData)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
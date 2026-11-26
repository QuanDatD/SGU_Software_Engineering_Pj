using Microsoft.Extensions.Logging;
using Zoo.DAL.Data;
using Zoo.DAL.Entities;
using Zoo.DAL.Enums;
using Zoo.DAL.Interfaces;

namespace Zoo.BUS.Services;

public class GenerationService(
    ZooDbContext db, ITranslationClient translator, ISpeechClient speechClient,
    IMediaStorage storage, ILogger<GenerationService> logger)
{
    public async Task GenerateAsync(
        NarrationRevision revision, NarrationTranslation source, GenerationJob? selectedJob, CancellationToken ct)
    {
        var languages = selectedJob == null ? PublicationRules.Languages : [selectedJob.Language];
        foreach (var language in languages)
        {
            var job = selectedJob ?? revision.Jobs.FirstOrDefault(j => j.Language == language);
            if (job?.Status == JobStatus.Processing &&
                (job.LockedUntilUtc == null || job.LockedUntilUtc > DateTime.UtcNow))
                continue;

            var translation = revision.Translations.FirstOrDefault(t => t.Language == language);
            if (translation == null)
            {
                translation = new NarrationTranslation { RevisionId = revision.Id, Language = language };
                db.NarrationTranslations.Add(translation);
            }

            if (job == null)
            {
                job = new GenerationJob { RevisionId = revision.Id, Language = language };
                db.GenerationJobs.Add(job);
            }

            await ProcessAsync(revision, source, translation, job, ct);
        }
    }

    private async Task ProcessAsync(NarrationRevision revision, NarrationTranslation source,
        NarrationTranslation translation, GenerationJob job, CancellationToken ct)
    {
        try
        {
            var needsTranslation = job.Language != "vi" &&
                (string.IsNullOrWhiteSpace(translation.ScriptText) || string.IsNullOrWhiteSpace(translation.Title) ||
                (string.IsNullOrWhiteSpace(translation.Description) && !string.IsNullOrWhiteSpace(source.Description)));
            var needsAudio = !await HasValidAudioAsync(translation, ct);
            if (needsTranslation || needsAudio || job.Status == JobStatus.Failed)
                job.AttemptCount++;

            if (needsTranslation)
            {
                await MarkProcessingAsync(job, JobStage.Translate, ct);
                var scriptMissing = string.IsNullOrWhiteSpace(translation.ScriptText);
                var title = await translator.TranslateAsync(source.Title, "vi", job.Language, ct);
                var script = scriptMissing
                    ? await translator.TranslateAsync(source.ScriptText, "vi", job.Language, ct)
                    : translation.ScriptText;
                var description = string.IsNullOrWhiteSpace(source.Description) ? "" :
                    await translator.TranslateAsync(source.Description, "vi", job.Language, ct);
                translation.Title = title;
                translation.ScriptText = script;
                translation.Description = description;
                translation.ContentHash = PublicationRules.Hash(script);
                if (scriptMissing || translation.SourceContentHash == null)
                    translation.SourceContentHash = PublicationRules.Hash(source.ScriptText);
            }

            if (needsAudio)
            {
                await MarkProcessingAsync(job, JobStage.Synthesize, ct);
                var audio = await speechClient.SynthesizeAsync(translation.ScriptText, job.Language, ct);
                if (audio.Length == 0) throw new InvalidOperationException("Azure trả về file rỗng.");
                job.Stage = JobStage.Store;
                translation.AudioPath = await storage.SaveAudioAsync(revision.PlaceId, revision.Id, job.Language, audio, ct);
                translation.AudioContentHash = PublicationRules.Hash(translation.ScriptText);
                translation.AudioStatus = AudioStatus.Ready;
            }

            if (!needsTranslation && !needsAudio && job.Status == JobStatus.Succeeded)
                return;

            translation.UpdatedAt = DateTime.UtcNow;
            job.Status = JobStatus.Succeeded;
            job.LastError = null;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            translation.AudioStatus = AudioStatus.Failed;
            job.Status = JobStatus.Failed;
            job.LastError = GenerationError(ex);
            logger.LogError(ex, "Generation failed for {RevisionId}/{Language}", revision.Id, job.Language);
        }

        job.LockedUntilUtc = null;
        job.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private async Task<bool> HasValidAudioAsync(NarrationTranslation translation, CancellationToken ct)
    {
        if (translation.AudioStatus != AudioStatus.Ready ||
            string.IsNullOrWhiteSpace(translation.AudioPath) ||
            string.IsNullOrWhiteSpace(translation.ScriptText) ||
            translation.AudioContentHash != PublicationRules.Hash(translation.ScriptText))
            return false;
        await using var audio = await storage.OpenReadAudioAsync(translation.AudioPath!, ct);
        return audio != null && audio.Length > 0;
    }

    private async Task MarkProcessingAsync(GenerationJob job, JobStage stage, CancellationToken ct)
    {
        job.Status = JobStatus.Processing;
        job.Stage = stage;
        job.LastError = null;
        job.UpdatedAt = DateTime.UtcNow;
        job.LockedUntilUtc = job.UpdatedAt.AddMinutes(10);
        await db.SaveChangesAsync(ct);
    }

    private static string GenerationError(Exception ex)
    {
        if (ex.Message == "Chưa cấu hình Azure Speech ApiKey." || ex.Message == "Chưa cấu hình Azure Translator ApiKey.")
            return ex.Message + " Cấu hình key bằng User Secrets rồi khởi động lại backend.";
        var match = System.Text.RegularExpressions.Regex.Match(ex.Message, @"^Lỗi Azure (Speech|Translator) \((\d{3})\)");
        if (match.Success)
        {
            var status = match.Groups[2].Value;
            var action = status switch
            {
                "401" => "Kiểm tra key và region có thuộc đúng tài nguyên Azure đang gọi.",
                "403" => "Kiểm tra quyền truy cập, trạng thái tài nguyên, mạng và hạn mức Azure.",
                "429" => "Đang bị giới hạn yêu cầu. Vui lòng thử lại sau.",
                "400" => "Azure từ chối nội dung hoặc cấu hình yêu cầu. Xem log backend để biết chi tiết.",
                _ => "Kiểm tra dịch vụ Azure và log backend để biết chi tiết."
            };
            return $"Azure {match.Groups[1].Value} HTTP {status}. {action}";
        }
        if (ex is OperationCanceledException) return "Dịch vụ Azure phản hồi quá thời gian chờ. Hãy thử lại.";
        if (ex is HttpRequestException) return "Không kết nối được Azure. Kiểm tra mạng, endpoint và region; xem log backend.";
        if (ex is IOException || ex is UnauthorizedAccessException) return "Không đọc/ghi được file audio. Kiểm tra thư mục storage và quyền ghi; xem log backend.";
        return "Không tạo được âm thanh. Xem log backend tại dòng Generation failed để biết nguyên nhân.";
    }
}

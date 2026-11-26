using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zoo.BUS.DTOs;
using Zoo.DAL.Data;
using Zoo.DAL.Entities;
using Zoo.DAL.Enums;
using Zoo.DAL.Interfaces;

namespace Zoo.BUS.Services;

public class GenerationService
{
    private static readonly string[] AllLangs = ["vi", "en", "ko", "zh", "ja"];
    private readonly ZooDbContext db;
    private readonly ITranslationClient translator;
    private readonly ISpeechClient speechClient;
    private readonly ILogger<GenerationService> logger;

    public GenerationService(
        ZooDbContext zooDb,
        ITranslationClient trans,
        ISpeechClient speech,
        ILogger<GenerationService> log)
    {
        db = zooDb;
        translator = trans;
        speechClient = speech;
        logger = log;
    }

    public async Task InitializeJobsAsync(Guid revisionId, CancellationToken ct)
    {
        var existing = await db.GenerationJobs.Where(j => j.RevisionId == revisionId).ToListAsync(ct);
        foreach (var lang in AllLangs)
        {
            if (!existing.Any(j => j.Language == lang))
            {
                db.GenerationJobs.Add(new GenerationJob
                {
                    RevisionId = revisionId,
                    Language = lang,
                    Status = JobStatus.Queued,
                    Stage = JobStage.Translate,
                    AttemptCount = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task ProcessJobAsync(Guid jobId, CancellationToken ct)
    {
        var job = await db.GenerationJobs
            .Include(j => j.Revision)
            .ThenInclude(r => r.Translations)
            .FirstOrDefaultAsync(j => j.Id == jobId, ct);

        if (job == null) return;

        try
        {
            job.Status = JobStatus.Processing;
            job.LockedUntilUtc = DateTime.UtcNow.AddMinutes(10);
            await db.SaveChangesAsync(ct);

            var trans = job.Revision.Translations.FirstOrDefault(t => t.Language == job.Language);
            if (trans == null)
            {
                job.Status = JobStatus.Failed;
                job.LastError = "Translation not found";
                await db.SaveChangesAsync(ct);
                return;
            }

            if (job.Stage == JobStage.Translate)
            {
                var viTrans = job.Revision.Translations.FirstOrDefault(t => t.Language == "vi");
                if (viTrans?.ScriptText != null && job.Language != "vi")
                {
                    trans.ScriptText = await translator.TranslateAsync(viTrans.ScriptText, "vi", job.Language, ct);
                }
                job.Stage = JobStage.Synthesize;
                job.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }

            if (job.Stage == JobStage.Synthesize)
            {
                if (string.IsNullOrWhiteSpace(trans.ScriptText))
                {
                    job.Status = JobStatus.Failed;
                    job.LastError = "ScriptText empty";
                    await db.SaveChangesAsync(ct);
                    return;
                }

                var audio = await speechClient.SynthesizeAsync(trans.ScriptText, job.Language, ct);
                if (audio != null && audio.Length > 0)
                {
                    job.Stage = JobStage.Store;
                    job.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(ct);
                    
                    trans.AudioPath = $"audio/{job.Revision.PlaceId}/{job.RevisionId}/{job.Language}.mp3";
                    trans.AudioContentHash = PublicationRules.Hash(trans.ScriptText);
                    trans.AudioStatus = AudioStatus.Ready;
                    trans.UpdatedAt = DateTime.UtcNow;

                    job.Status = JobStatus.Succeeded;
                    job.LockedUntilUtc = null;
                    job.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(ct);
                }
                else
                {
                    job.Status = JobStatus.Failed;
                    job.LastError = "Audio synthesis failed";
                    await db.SaveChangesAsync(ct);
                }
            }
        }
        catch (Exception ex)
        {
            job.Status = JobStatus.Failed;
            job.LastError = ex.Message;
            job.LockedUntilUtc = null;
            job.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task RetryJobAsync(Guid jobId, CancellationToken ct)
    {
        var job = await db.GenerationJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job == null) return;

        job.Status = JobStatus.Queued;
        job.Stage = JobStage.Translate;
        job.AttemptCount += 1;
        job.LockedUntilUtc = null;
        job.LastError = null;
        job.UpdatedAt = DateTime.UtcNow;
        
        await db.SaveChangesAsync(ct);
    }
}

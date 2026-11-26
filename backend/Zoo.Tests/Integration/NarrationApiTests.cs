using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Zoo.BUS.Services;
using Zoo.DAL.Entities;
using Zoo.DAL.Enums;
using Zoo.DAL.Interfaces;

namespace Zoo.Tests.Integration;

public sealed partial class ApiTests
{
    [SqlFact]
    public async Task Br14_DeleteRejectsDraftWithProcessingJob()
    {
        using var client = fixture.CreateClient();
        await LoginAsync(client, "admin", SeedPassword);
        var revisionId = await CreateNarrationAsync(client);
        await AddProcessingJobAsync(revisionId);

        using var rejected = await WriteAsync(client, HttpMethod.Delete, RevisionPath(revisionId));

        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        await using var db = fixture.OpenDatabase();
        Assert.True(await db.NarrationRevisions.AnyAsync(r => r.Id == revisionId));
        Assert.True(await db.GenerationJobs.AnyAsync(j => j.RevisionId == revisionId && j.Status == JobStatus.Processing));

        var job = await db.GenerationJobs.SingleAsync(j => j.RevisionId == revisionId);
        job.Status = JobStatus.Failed;
        await db.SaveChangesAsync();
        using var deleted = await WriteAsync(client, HttpMethod.Delete, RevisionPath(revisionId));
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.False(await db.NarrationRevisions.AnyAsync(r => r.Id == revisionId));
    }

    [SqlFact]
    public async Task Br14_TranslationUpdateRejectsDraftWithProcessingJob()
    {
        using var client = fixture.CreateClient();
        await LoginAsync(client, "admin", SeedPassword);
        var revisionId = await CreateNarrationAsync(client);
        await AddProcessingJobAsync(revisionId);

        using var rejected = await UpdateScriptAsync(client, revisionId, "vi", "Unauthorized mid-job change");

        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        await using var db = fixture.OpenDatabase();
        Assert.Equal("CI narration", (await db.NarrationTranslations.SingleAsync(t => t.RevisionId == revisionId)).ScriptText);
        Assert.True(await db.GenerationJobs.AnyAsync(j => j.RevisionId == revisionId && j.Status == JobStatus.Processing));
    }

    [SqlFact]
    public async Task Br15_SourceChangePreservesExistingTranslation()
    {
        using var client = fixture.CreateClient();
        await LoginAsync(client, "admin", SeedPassword);
        var revisionId = await CreateNarrationAsync(client);
        using var translated = await UpdateScriptAsync(client, revisionId, "en", "Manually reviewed translation");
        translated.EnsureSuccessStatusCode();

        using var sourceChanged = await UpdateScriptAsync(client, revisionId, "vi", "Changed Vietnamese source");
        sourceChanged.EnsureSuccessStatusCode();

        await using var db = fixture.OpenDatabase();
        var scripts = await db.NarrationTranslations.Where(t => t.RevisionId == revisionId).ToDictionaryAsync(t => t.Language);
        Assert.Equal("Changed Vietnamese source", scripts["vi"].ScriptText);
        Assert.Equal("Manually reviewed translation", scripts["en"].ScriptText);
        Assert.True(scripts["en"].IsEditedByAdmin);
    }

    [SqlFact]
    public async Task Br15_SavingTheSameScriptKeepsAudioValid()
    {
        using var client = fixture.CreateClient();
        await LoginAsync(client, "admin", SeedPassword);
        var revisionId = await CreateNarrationAsync(client);
        await GenerateAsync(client, revisionId);
        await using var db = fixture.OpenDatabase();
        var before = await LoadTranslationAsync(db, revisionId, "en");
        Assert.True(PublicationRules.Ready(before));

        using var saved = await UpdateScriptAsync(client, revisionId, "en", before.ScriptText);
        saved.EnsureSuccessStatusCode();

        var after = await LoadTranslationAsync(db, revisionId, "en");
        Assert.True(after.IsEditedByAdmin);
        Assert.True(PublicationRules.Ready(after));
        Assert.Equal(before.AudioPath, after.AudioPath);
        Assert.Equal(before.AudioContentHash, after.AudioContentHash);
    }

    [SqlFact]
    public async Task Tc24_ChangedScriptInvalidatesOnlyItsOwnAudio()
    {
        using var client = fixture.CreateClient();
        await LoginAsync(client, "admin", SeedPassword);
        var revisionId = await CreateNarrationAsync(client);
        await GenerateAsync(client, revisionId);
        await using var db = fixture.OpenDatabase();
        var before = await LoadTranslationAsync(db, revisionId, "vi");
        Assert.True(PublicationRules.Ready(before));

        using var saved = await UpdateScriptAsync(client, revisionId, "en", "Changed English script");
        saved.EnsureSuccessStatusCode();

        var english = await LoadTranslationAsync(db, revisionId, "en");
        var vietnamese = await LoadTranslationAsync(db, revisionId, "vi");
        Assert.Equal("Changed English script", english.ScriptText);
        Assert.True(english.IsEditedByAdmin);
        Assert.False(PublicationRules.Ready(english));
        Assert.True(PublicationRules.Ready(vietnamese));
        Assert.Equal(before.AudioPath, vietnamese.AudioPath);
        Assert.Equal(before.AudioContentHash, vietnamese.AudioContentHash);
    }

    [SqlFact]
    public Task Tc26_PublishRejectsHashMismatch() => AssertCorruptAudioBlocksPublicationAsync(removeFile: false);

    [SqlFact]
    public Task Tc26_PublishRejectsMissingAudioFile() => AssertCorruptAudioBlocksPublicationAsync(removeFile: true);

    [SqlFact]
    public async Task Br17_PublishRejectsDraftWithUnfinishedJobEvenWhenAudioIsReady()
    {
        using var client = fixture.CreateClient();
        await LoginAsync(client, "admin", SeedPassword);
        var revisionId = await CreateNarrationAsync(client);
        await GenerateAsync(client, revisionId);
        await using var db = fixture.OpenDatabase();
        var job = await db.GenerationJobs.SingleAsync(j => j.RevisionId == revisionId && j.Language == "en");
        job.Status = JobStatus.Processing;
        await db.SaveChangesAsync();

        using var rejected = await PublishAsync(client, revisionId);

        Assert.False(rejected.IsSuccessStatusCode);
        Assert.Contains(rejected.StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.Conflict });
        await AssertUnpublishedAsync(db, revisionId);
    }

    [SqlFact]
    public async Task Br27_RetryTargetsOneExistingJobAndIncrementsAttemptCount()
    {
        using var client = fixture.CreateClient();
        await LoginAsync(client, "admin", SeedPassword);
        var revisionId = await CreateNarrationAsync(client);
        var synthesisCalls = new List<string>();
        fixture.Azure.BeforeSynthesis = (language, _) => language is "en" or "ko"
            ? Task.FromException(new HttpRequestException("Simulated provider failure."))
            : Task.CompletedTask;
        try
        {
            await GenerateAsync(client, revisionId);
            await using var db = fixture.OpenDatabase();
            var before = await db.GenerationJobs.AsNoTracking().Where(j => j.RevisionId == revisionId)
                .ToDictionaryAsync(j => j.Language);
            Assert.Equal(JobStatus.Failed, before["en"].Status);
            Assert.Equal(JobStatus.Failed, before["ko"].Status);
            var successfulAudio = await LoadTranslationAsync(db, revisionId, "vi");
            Assert.True(PublicationRules.Ready(successfulAudio));

            fixture.Azure.BeforeSynthesis = (language, _) =>
            {
                synthesisCalls.Add(language);
                return Task.CompletedTask;
            };
            using var retried = await WriteAsync(client, HttpMethod.Post, $"/api/admin/jobs/{before["en"].Id}/retry");
            retried.EnsureSuccessStatusCode();

            var after = await db.GenerationJobs.AsNoTracking().Where(j => j.RevisionId == revisionId)
                .ToDictionaryAsync(j => j.Language);
            var retainedAudio = await LoadTranslationAsync(db, revisionId, "vi");
            Assert.Multiple(
                () => Assert.Equal(before.Values.Select(j => j.Id).Order(), after.Values.Select(j => j.Id).Order()),
                () => Assert.Equal(JobStatus.Succeeded, after["en"].Status),
                () => Assert.Equal(before["en"].AttemptCount + 1, after["en"].AttemptCount),
                () => Assert.Equal(JobStatus.Failed, after["ko"].Status),
                () => Assert.Equal(before["ko"].AttemptCount, after["ko"].AttemptCount),
                () => Assert.Equal(new[] { "en" }, synthesisCalls),
                () => Assert.Equal(successfulAudio.AudioPath, retainedAudio.AudioPath),
                () => Assert.Equal(successfulAudio.AudioContentHash, retainedAudio.AudioContentHash),
                () => Assert.True(PublicationRules.Ready(retainedAudio)));
        }
        finally
        {
            fixture.Azure.BeforeSynthesis = null;
        }
    }

    [SqlFact]
    public async Task Tc25_PreviewStreamsReadyAudioOnlyForAdmin()
    {
        using var admin = fixture.CreateClient();
        using var visitor = fixture.CreateClient();
        await LoginAsync(admin, "admin", SeedPassword);
        var revisionId = await CreateNarrationAsync(admin);
        var url = $"{RevisionPath(revisionId)}/audio/en";
        using var noAudio = await admin.GetAsync(url);
        Assert.Equal(HttpStatusCode.NotFound, noAudio.StatusCode);
        await GenerateAsync(admin, revisionId);

        using var denied = await visitor.GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        using var preview = await admin.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
        Assert.Equal("audio/mpeg", preview.Content.Headers.ContentType?.MediaType);
        Assert.NotEmpty(await preview.Content.ReadAsByteArrayAsync());

        using var stalePreview = await admin.GetAsync($"{url}?hash=stale");
        Assert.Equal(HttpStatusCode.BadRequest, stalePreview.StatusCode);
    }

    private async Task AssertCorruptAudioBlocksPublicationAsync(bool removeFile)
    {
        using var client = fixture.CreateClient();
        await LoginAsync(client, "admin", SeedPassword);
        var revisionId = await CreateNarrationAsync(client);
        await GenerateAsync(client, revisionId);
        await using var db = fixture.OpenDatabase();
        var translation = await db.NarrationTranslations.SingleAsync(t => t.RevisionId == revisionId && t.Language == "en");
        Assert.True(PublicationRules.Ready(translation));
        if (removeFile)
        {
            await using var scope = fixture.Services.CreateAsyncScope();
            var storage = scope.ServiceProvider.GetRequiredService<IMediaStorage>();
            await using var stream = Assert.IsType<FileStream>(await storage.OpenReadAudioAsync(translation.AudioPath!));
            var path = stream.Name;
            await stream.DisposeAsync();
            File.Delete(path);
        }
        else
        {
            translation.AudioContentHash = "hash-from-an-older-script";
            await db.SaveChangesAsync();
        }

        using var rejected = await PublishAsync(client, revisionId);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Contains("en", await rejected.Content.ReadAsStringAsync());
        await AssertUnpublishedAsync(db, revisionId);
    }

    private async Task AddProcessingJobAsync(Guid revisionId)
    {
        await using var db = fixture.OpenDatabase();
        db.GenerationJobs.Add(new GenerationJob
        {
            RevisionId = revisionId, Language = "vi", Stage = JobStage.Synthesize,
            Status = JobStatus.Processing, LockedUntilUtc = DateTime.UtcNow.AddMinutes(5)
        });
        await db.SaveChangesAsync();
    }

    private static async Task AssertUnpublishedAsync(Zoo.DAL.Data.ZooDbContext db, Guid revisionId)
    {
        var revision = await db.NarrationRevisions.AsNoTracking().Include(r => r.Place)
            .SingleAsync(r => r.Id == revisionId);
        Assert.Equal(RevisionStatus.Draft, revision.Status);
        Assert.Null(revision.Place.PublishedRevisionId);
    }

    private static Task<NarrationTranslation> LoadTranslationAsync(
        Zoo.DAL.Data.ZooDbContext db, Guid revisionId, string language) =>
        db.NarrationTranslations.AsNoTracking().SingleAsync(t => t.RevisionId == revisionId && t.Language == language);

    private static async Task<Guid> CreateNarrationAsync(HttpClient client)
    {
        var placeId = await CreatePlaceAsync(client);
        using var draft = await WriteAsync(client, HttpMethod.Post, $"{AdminPlaces}/{placeId}/revisions",
            JsonContent.Create(new { sourceText = "CI narration" }));
        return (await ReadAsync(draft)).GetProperty("id").GetGuid();
    }

    private static Task<HttpResponseMessage> PublishAsync(HttpClient client, Guid revisionId) =>
        WriteAsync(client, HttpMethod.Post, $"{RevisionPath(revisionId)}/publish",
            JsonContent.Create(new Zoo.BUS.DTOs.PublishRevisionRequest
            {
                ConfirmAllLanguagesReady = true,
                ConfirmAllAudioValid = true,
                ConfirmAllJobsCompleted = true,
                ConfirmAllTranslationsReviewed = true
            }));

    private static string RevisionPath(Guid revisionId) => $"/api/admin/revisions/{revisionId}";

    private static Task<HttpResponseMessage> UpdateScriptAsync(HttpClient client, Guid revisionId, string lang, string text)
        => WriteAsync(client, HttpMethod.Put, $"{RevisionPath(revisionId)}/translations/{lang}", JsonContent.Create(new { text }));

    private static async Task GenerateAsync(HttpClient client, Guid revisionId)
    {
        using var response = await WriteAsync(client, HttpMethod.Post, $"{RevisionPath(revisionId)}/generate");
        response.EnsureSuccessStatusCode();
    }
}

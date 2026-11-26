using System.Net;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Zoo.DAL.Entities;
using Zoo.DAL.Enums;

namespace Zoo.Tests.Integration;

public sealed partial class ApiTests
{
    [SqlFact]
    public async Task Br27_RepeatedGenerateDoesNotCallProviderForProcessingJob()
    {
        using var client = fixture.CreateClient();
        await LoginAsync(client, "admin", SeedPassword);
        var revisionId = await CreateNarrationAsync(client);
        await AddProcessingJobAsync(revisionId);
        await using var db = fixture.OpenDatabase();
        var original = await db.GenerationJobs.AsNoTracking().SingleAsync(j => j.RevisionId == revisionId);
        var synthesis = new List<string>();
        fixture.Azure.BeforeSynthesis = (language, _) =>
        {
            synthesis.Add(language);
            return Task.CompletedTask;
        };

        try
        {
            using var repeated = await WriteAsync(client, HttpMethod.Post, $"{RevisionPath(revisionId)}/generate");
            Assert.True(repeated.IsSuccessStatusCode || repeated.StatusCode == HttpStatusCode.Conflict);
            var existing = await db.GenerationJobs.AsNoTracking().Where(j => j.RevisionId == revisionId && j.Language == "vi")
                .ToListAsync();
            Assert.Multiple(
                () => Assert.DoesNotContain("vi", synthesis),
                () => Assert.Equal(original.Id, Assert.Single(existing).Id),
                () => Assert.Equal(JobStatus.Processing, Assert.Single(existing).Status));
        }
        finally
        {
            fixture.Azure.BeforeSynthesis = null;
        }
    }

    [SqlFact]
    public async Task Tc33_RestartRequeuesInterruptedJobsAndKeepsActiveLease()
    {
        await using var api = new SqlApiFixture();
        using var original = api.CreateClient();
        await LoginAsync(original, "admin", SeedPassword);
        var revisionId = await CreateNarrationAsync(original);

        await using var db = api.OpenDatabase();
        var now = DateTime.UtcNow;
        db.GenerationJobs.AddRange(
            Job("vi", now.AddMinutes(-5)),
            Job("en", null),
            Job("zh", now.AddHours(1)));
        await db.SaveChangesAsync();

        await using var restartedFactory = api.WithWebHostBuilder(_ => { });
        using var restarted = restartedFactory.CreateClient();
        await LoginAsync(restarted, "admin", SeedPassword);
        var jobs = await db.GenerationJobs.AsNoTracking().Where(j => j.RevisionId == revisionId)
            .ToDictionaryAsync(j => j.Language);

        Assert.Equal(JobStatus.Queued, jobs["vi"].Status);
        Assert.Equal(JobStatus.Queued, jobs["en"].Status);
        Assert.Equal(JobStatus.Processing, jobs["zh"].Status);

        GenerationJob Job(string language, DateTime? lease) => new()
        {
            RevisionId = revisionId, Language = language, Stage = JobStage.Synthesize,
            Status = JobStatus.Processing, LockedUntilUtc = lease
        };
    }
}

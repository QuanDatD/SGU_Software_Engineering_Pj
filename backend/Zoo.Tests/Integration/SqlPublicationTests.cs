using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Zoo.DAL.Data;
using Zoo.DAL.Entities;
using Zoo.DAL.Enums;

namespace Zoo.Tests.Integration;

[Collection("SQL-backed API")]
[Trait("Category", "SQL")]
public sealed class SqlPublicationTests(SqlApiFixture fixture)
{
    [SqlFact]
    public async Task Br27_SqlRejectsDuplicateJobForRevisionAndLanguage()
    {
        using var client = fixture.CreateClient();
        await using var db = fixture.OpenDatabase();
        var revision = await InsertRevisionAsync(db, RevisionStatus.Draft);
        var otherRevision = await InsertRevisionAsync(db, RevisionStatus.Draft);

        Task InsertJobAsync(Guid revisionId, string language) =>
            db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO dbo.GenerationJobs
                    (Id, RevisionId, Language, Stage, Status, AttemptCount, CreatedAt, UpdatedAt)
                VALUES ({Guid.NewGuid()}, {revisionId}, {language}, 0, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME())
                """);

        await InsertJobAsync(revision.Id, "en");
        await InsertJobAsync(revision.Id, "vi");
        await InsertJobAsync(otherRevision.Id, "en");

        var error = await Assert.ThrowsAsync<SqlException>(() => InsertJobAsync(revision.Id, "en"));
        Assert.Contains(error.Number, new[] { 2601, 2627 });
        Assert.Equal(1, await db.GenerationJobs.CountAsync(j => j.RevisionId == revision.Id && j.Language == "en"));
    }

    [SqlFact]
    public async Task Br18_SqlRejectsPublishedPointerToAnotherPlace()
    {
        using var client = fixture.CreateClient();
        await using var db = fixture.OpenDatabase();
        var current = await InsertRevisionAsync(db, RevisionStatus.Published);
        var foreign = await InsertRevisionAsync(db, RevisionStatus.Published);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE dbo.Places SET PublishedRevisionId = {current.Id} WHERE Id = {current.PlaceId}");

        await AssertSqlRejectsAsync(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE dbo.Places SET PublishedRevisionId = {foreign.Id} WHERE Id = {current.PlaceId}"));

        Assert.Equal(current.Id, (await db.Places.AsNoTracking().SingleAsync(p => p.Id == current.PlaceId))
            .PublishedRevisionId);
    }

    [SqlFact]
    public async Task Br18_SqlRejectsPublicPointerToDraftOfSamePlace()
    {
        using var client = fixture.CreateClient();
        await using var db = fixture.OpenDatabase();
        var draft = await InsertRevisionAsync(db, RevisionStatus.Draft);

        await AssertSqlRejectsAsync(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE dbo.Places SET PublishedRevisionId = {draft.Id} WHERE Id = {draft.PlaceId}"));

        Assert.Null((await db.Places.AsNoTracking().SingleAsync(p => p.Id == draft.PlaceId)).PublishedRevisionId);
    }

    [SqlFact]
    public async Task Br18_SqlRejectsTwoPublishedRevisionsForSamePlace()
    {
        using var client = fixture.CreateClient();
        await using var db = fixture.OpenDatabase();
        var published = await InsertRevisionAsync(db, RevisionStatus.Published);
        var draft = new NarrationRevision { PlaceId = published.PlaceId, Version = 2 };
        db.NarrationRevisions.Add(draft);
        await db.SaveChangesAsync();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE dbo.Places SET PublishedRevisionId = {published.Id} WHERE Id = {published.PlaceId}");

        var error = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE dbo.NarrationRevisions SET Status = {(int)RevisionStatus.Published}, PublishedAt = SYSUTCDATETIME() WHERE Id = {draft.Id}"));

        Assert.Contains(error.Errors.Cast<SqlError>(), item => item.Number is 2601 or 2627 or 547 or 3609 || item.Number >= 50000);
        Assert.Equal(1, await db.NarrationRevisions.CountAsync(r =>
            r.PlaceId == published.PlaceId && r.Status == RevisionStatus.Published));
    }

    [SqlFact]
    public Task Br14_SqlRejectsScriptUpdatesToPublished() => AssertScriptLockedAsync(RevisionStatus.Published);

    [SqlFact]
    public Task Br14_SqlRejectsScriptUpdatesToArchived() => AssertScriptLockedAsync(RevisionStatus.Archived);

    [SqlFact]
    public Task Br14_SqlRejectsDeletionOfPublished() => AssertDeletionLockedAsync(RevisionStatus.Published);

    [SqlFact]
    public Task Br14_SqlRejectsDeletionOfArchived() => AssertDeletionLockedAsync(RevisionStatus.Archived);

    private async Task AssertScriptLockedAsync(RevisionStatus status)
    {
        using var client = fixture.CreateClient();
        await using var db = fixture.OpenDatabase();
        var revision = await InsertRevisionAsync(db, status);
        var translationId = Assert.Single(revision.Translations).Id;

        await AssertSqlRejectsAsync(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE dbo.NarrationTranslations SET ScriptText = N'Changed in SQL' WHERE Id = {translationId}"));

        var persisted = await db.NarrationTranslations.AsNoTracking().SingleAsync(t => t.Id == translationId);
        Assert.Equal("Original script", persisted.ScriptText);
    }

    private async Task AssertDeletionLockedAsync(RevisionStatus status)
    {
        using var client = fixture.CreateClient();
        await using var db = fixture.OpenDatabase();
        var revision = await InsertRevisionAsync(db, status);

        // Omit the publication pointer to exercise the status rule independently of its FK.
        await AssertSqlRejectsAsync(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM dbo.NarrationRevisions WHERE Id = {revision.Id}"));

        Assert.True(await db.NarrationRevisions.AnyAsync(r => r.Id == revision.Id && r.Status == status));
        Assert.True(await db.NarrationTranslations.AnyAsync(t => t.RevisionId == revision.Id));
    }

    private static async Task AssertSqlRejectsAsync(Func<Task> write)
    {
        var error = await Assert.ThrowsAsync<SqlException>(write);
        Assert.Contains(error.Errors.Cast<SqlError>(), item => item.Number is 547 or 3609 || item.Number >= 50000);
    }

    private static async Task<NarrationRevision> InsertRevisionAsync(ZooDbContext db, RevisionStatus status)
    {
        var place = new Place { Code = $"CI_{Guid.NewGuid():N}" };
        var revision = new NarrationRevision
        {
            PlaceId = place.Id,
            Version = 1,
            Status = status,
            PublishedAt = status == RevisionStatus.Draft ? null : DateTime.UtcNow
        };
        revision.Translations.Add(new NarrationTranslation { Language = "vi", ScriptText = "Original script" });
        place.Revisions.Add(revision);
        db.Places.Add(place);
        await db.SaveChangesAsync();
        return revision;
    }
}

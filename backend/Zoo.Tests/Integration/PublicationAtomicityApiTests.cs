using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Zoo.DAL.Enums;

namespace Zoo.Tests.Integration;

public sealed partial class ApiTests
{
    [SqlFact]
    public async Task Br18_FailedPublicPointerWriteRollsBackPublication()
    {
        await using var api = new SqlApiFixture();
        using var admin = api.CreateClient();
        using var visitor = api.CreateClient();
        await LoginAsync(admin, "admin", SeedPassword);
        var oldId = await CreateNarrationAsync(admin);
        await GenerateAsync(admin, oldId);
        using var first = await PublishAsync(admin, oldId);
        first.EnsureSuccessStatusCode();

        await using var db = api.OpenDatabase();
        var placeId = await db.NarrationRevisions.Where(r => r.Id == oldId).Select(r => r.PlaceId).SingleAsync();
        using var draft = await WriteAsync(admin, HttpMethod.Post, $"{AdminPlaces}/{placeId}/revisions",
            JsonContent.Create(new { sourceText = "Replacement narration for rollback test" }));
        var newId = (await ReadAsync(draft)).GetProperty("id").GetGuid();
        await GenerateAsync(admin, newId);

        // The isolated database rejects only the candidate public pointer.
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            DECLARE @statement nvarchar(max) = 'ALTER TABLE dbo.Places WITH CHECK
                ADD CONSTRAINT CK_CI_RejectPublicPointer
                CHECK (PublishedRevisionId <> CONVERT(uniqueidentifier, '
                + QUOTENAME(CONVERT(nvarchar(36), {newId}), '''') + '));';
            EXEC sys.sp_executesql @statement;
            """);

        using var failed = await PublishAsync(admin, newId);
        Assert.False(failed.IsSuccessStatusCode);

        var place = await db.Places.AsNoTracking().SingleAsync(p => p.Id == placeId);
        Assert.Equal(oldId, place.PublishedRevisionId);
        var versions = await db.NarrationRevisions.AsNoTracking().Where(r => r.PlaceId == placeId)
            .ToDictionaryAsync(r => r.Id, r => r.Status);
        Assert.Equal(RevisionStatus.Published, versions[oldId]);
        Assert.Equal(RevisionStatus.Draft, versions[newId]);
        using var publicVersion = await visitor.GetAsync($"/api/places/{placeId}");
        Assert.Equal(oldId, (await ReadAsync(publicVersion)).GetProperty("revisionId").GetGuid());
    }
}

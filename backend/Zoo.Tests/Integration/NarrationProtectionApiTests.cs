using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Zoo.BUS.Services;
using Zoo.DAL.Enums;

namespace Zoo.Tests.Integration;

public sealed partial class ApiTests
{
    [SqlFact]
    public async Task Br16_GenerationUsesManuallyEditedScript()
    {
        using var client = fixture.CreateClient();
        await LoginAsync(client, "admin", SeedPassword);
        var revisionId = await CreateNarrationAsync(client);
        const string script = "English script edited by an admin";
        using var edited = await UpdateScriptAsync(client, revisionId, "en", script);
        edited.EnsureSuccessStatusCode();

        await GenerateAsync(client, revisionId);

        await using var db = fixture.OpenDatabase();
        var translation = await LoadTranslationAsync(db, revisionId, "en");
        Assert.Equal(script, translation.ScriptText);
        Assert.True(translation.IsEditedByAdmin);
        Assert.True(PublicationRules.Ready(translation));
    }

    [SqlFact]
    public async Task Tc21_ApiRejectsEditsAndDeletesOfPublishedAndArchived()
    {
        using var client = fixture.CreateClient();
        await LoginAsync(client, "admin", SeedPassword);
        var publishedId = await CreateNarrationAsync(client);
        await GenerateAsync(client, publishedId);
        using var firstPublication = await PublishAsync(client, publishedId);
        firstPublication.EnsureSuccessStatusCode();

        await using var db = fixture.OpenDatabase();
        var placeId = await db.NarrationRevisions.Where(r => r.Id == publishedId).Select(r => r.PlaceId).SingleAsync();
        using var draft = await WriteAsync(client, HttpMethod.Post, $"{AdminPlaces}/{placeId}/revisions",
            JsonContent.Create(new { sourceText = "Replacement script" }));
        var replacementId = (await ReadAsync(draft)).GetProperty("id").GetGuid();
        await GenerateAsync(client, replacementId);
        using var secondPublication = await PublishAsync(client, replacementId);
        secondPublication.EnsureSuccessStatusCode();

        foreach (var (id, status, originalScript) in new[]
        {
            (publishedId, RevisionStatus.Archived, "CI narration"),
            (replacementId, RevisionStatus.Published, "Replacement script")
        })
        {
            using var edited = await UpdateScriptAsync(client, id, "vi", "Attempted overwrite");
            using var deleted = await WriteAsync(client, HttpMethod.Delete, RevisionPath(id));
            Assert.Equal(HttpStatusCode.Conflict, edited.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, deleted.StatusCode);
            Assert.Equal(status, (await db.NarrationRevisions.AsNoTracking().SingleAsync(r => r.Id == id)).Status);
            Assert.Equal(originalScript, (await LoadTranslationAsync(db, id, "vi")).ScriptText);
        }
    }
}

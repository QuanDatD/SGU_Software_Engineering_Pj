using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Zoo.DAL.Entities;
using Zoo.DAL.Enums;

namespace Zoo.Tests.Integration;

[Collection("SQL-backed API")]
[Trait("Category", "API")]
public sealed class ApiTests(SqlApiFixture fixture)
{
    private const string SeedPassword = "Admin@123";
    private const string AdminPlaces = "/api/admin/places";

    [SqlFact]
    public async Task Tc16_Tc35_AdminRequiresValidCredentialsAndCsrf()
    {
        using var client = fixture.CreateClient();
        using var anonymous = await client.GetAsync(AdminPlaces);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        using var missingCsrf = await client.PostAsJsonAsync("/api/auth/login",
            new { username = "admin", password = SeedPassword });
        Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
        using var invalid = await WriteAsync(client, HttpMethod.Post, "/api/auth/login",
            JsonContent.Create(new { username = "admin", password = "wrong" }));
        Assert.Equal(HttpStatusCode.Unauthorized, invalid.StatusCode);
        await LoginAsync(client, "admin", SeedPassword);
        using var authorized = await client.GetAsync(AdminPlaces);
        Assert.Equal(HttpStatusCode.OK, authorized.StatusCode);
    }

    [SqlFact]
    public async Task Tc04_ConcurrentQrRequestsCountOnlyOnce()
    {
        using var client = fixture.CreateClient();
        var sessionId = Guid.NewGuid().ToString();
        var payload = new { sessionId, isDemo = true };
        var responses = await Task.WhenAll(
            client.PostAsJsonAsync("/api/visit/qr", payload),
            client.PostAsJsonAsync("/api/visit/qr", payload));
        var results = new List<bool>();
        foreach (var response in responses)
        {
            using (response)
                results.Add((await ReadAsync(response)).GetProperty("recorded").GetBoolean());
        }
        Assert.Single(results, recorded => recorded);
        await using var db = fixture.OpenDatabase();
        Assert.True((await db.QrVisits.SingleAsync(v => v.SessionId == sessionId)).IsDemo);
    }

    [SqlFact]
    public async Task Tc18_DeleteThroughApiRetainsPlaceAndDraft()
    {
        using var client = fixture.CreateClient();
        await LoginAsync(client, "admin", SeedPassword);
        var placeId = await CreatePlaceAsync(client);
        using var response = await WriteAsync(client, HttpMethod.Delete, $"{AdminPlaces}/{placeId}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("ẩn", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());
        await using var db = fixture.OpenDatabase();
        Assert.True(await db.Places.AnyAsync(p => p.Id == placeId));
        Assert.Single(await db.NarrationRevisions.Where(r => r.PlaceId == placeId).ToListAsync());
    }

    [SqlFact]
    public async Task Uc05_CreateDraftRecordsAdminAndUpdateByDifferentAdminKeepsCreator()
    {
        using var creator = fixture.CreateClient();
        var creatorId = await LoginAsync(creator, "admin", SeedPassword);
        var placeId = await CreatePlaceAsync(creator);
        await using var db = fixture.OpenDatabase();
        var original = await db.NarrationRevisions.AsNoTracking().SingleAsync(r => r.PlaceId == placeId);

        var editor = new AdminUser { Username = $"ci-{Guid.NewGuid():N}", Role = "Admin", IsActive = true };
        editor.PasswordHash = new PasswordHasher<AdminUser>().HashPassword(editor, SeedPassword);
        db.AdminUsers.Add(editor);
        await db.SaveChangesAsync();
        using var editorClient = fixture.CreateClient();
        Assert.NotEqual(creatorId, await LoginAsync(editorClient, editor.Username, SeedPassword));
        using var updated = await WriteAsync(editorClient, HttpMethod.Put, $"{AdminPlaces}/{placeId}",
            PlaceForm("Edited by another admin"));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var persisted = await db.NarrationRevisions.AsNoTracking().SingleAsync(r => r.PlaceId == placeId);
        Assert.Equal(original.Id, persisted.Id);
        Assert.Equal(creatorId, original.CreatedByAdminId);
        Assert.Equal(original.CreatedByAdminId, persisted.CreatedByAdminId);
    }

    [SqlFact]
    public async Task Tc06_Tc26_Tc27_PublicEndpointShowsOnlyThePublishedRevision()
    {
        using var admin = fixture.CreateClient();
        using var visitor = fixture.CreateClient();
        await LoginAsync(admin, "admin", SeedPassword);
        var placeId = await CreatePlaceAsync(admin);
        var publicPath = $"/api/places/{placeId}";
        using var draftOnly = await visitor.GetAsync(publicPath);
        Assert.Equal(HttpStatusCode.NotFound, draftOnly.StatusCode);

        async Task<Guid> CreateDraftAsync(string sourceText)
        {
            using var response = await WriteAsync(admin, HttpMethod.Post, $"{AdminPlaces}/{placeId}/revisions",
                JsonContent.Create(new { sourceText }));
            return (await ReadAsync(response)).GetProperty("id").GetGuid();
        }

        async Task GenerateAndPublishAsync(Guid revisionId)
        {
            var path = $"/api/admin/revisions/{revisionId}";
            using var generated = await WriteAsync(admin, HttpMethod.Post, $"{path}/generate");
            generated.EnsureSuccessStatusCode();
            using var published = await WriteAsync(admin, HttpMethod.Post, $"{path}/publish");
            published.EnsureSuccessStatusCode();
            using var current = await visitor.GetAsync(publicPath);
            Assert.Equal(revisionId, (await ReadAsync(current)).GetProperty("revisionId").GetGuid());
        }

        var first = await CreateDraftAsync("First public narration");
        using var missingAudio = await WriteAsync(admin, HttpMethod.Post, $"/api/admin/revisions/{first}/publish");
        Assert.Equal(HttpStatusCode.BadRequest, missingAudio.StatusCode);
        await GenerateAndPublishAsync(first);

        var second = await CreateDraftAsync("Replacement narration");
        using var beforePublishing = await visitor.GetAsync(publicPath);
        Assert.Equal(first, (await ReadAsync(beforePublishing)).GetProperty("revisionId").GetGuid());
        await GenerateAndPublishAsync(second);

        await using var db = fixture.OpenDatabase();
        Assert.Equal(RevisionStatus.Archived, (await db.NarrationRevisions.SingleAsync(r => r.Id == first)).Status);
        Assert.Equal(RevisionStatus.Published, (await db.NarrationRevisions.SingleAsync(r => r.Id == second)).Status);
        Assert.Equal(second, (await db.Places.SingleAsync(p => p.Id == placeId)).PublishedRevisionId);
    }

    [SqlFact]
    public async Task Tc22_Tc23_JobIsPersistedBeforeAzureAndRetryKeepsExistingJobs()
    {
        using var client = fixture.CreateClient();
        await LoginAsync(client, "admin", SeedPassword);
        var placeId = await CreatePlaceAsync(client);
        using var draft = await WriteAsync(client, HttpMethod.Post, $"{AdminPlaces}/{placeId}/revisions",
            JsonContent.Create(new { sourceText = "CI narration" }));
        var revisionId = (await ReadAsync(draft)).GetProperty("id").GetGuid();
        var path = $"/api/admin/revisions/{revisionId}";
        var observed = new List<(string Language, JobStage Stage, JobStatus? Persisted)>();

        async Task ObserveAsync(string language, JobStage stage, CancellationToken ct)
        {
            await using var db = fixture.OpenDatabase();
            var job = await db.GenerationJobs.AsNoTracking()
                .SingleOrDefaultAsync(j => j.RevisionId == revisionId && j.Language == language, ct);
            observed.Add((language, stage, job?.Stage == stage ? job.Status : null));
        }

        fixture.Azure.BeforeSynthesis = (lang, ct) => ObserveAsync(lang, JobStage.Synthesize, ct);
        fixture.Azure.BeforeTranslation = (lang, ct) => observed.Any(o => o.Language == lang && o.Stage == JobStage.Translate)
            ? Task.CompletedTask : ObserveAsync(lang, JobStage.Translate, ct);
        fixture.Azure.FailedLanguage = "en";
        try
        {
            using var generated = await WriteAsync(client, HttpMethod.Post, $"{path}/generate");
            generated.EnsureSuccessStatusCode();
            await using var db = fixture.OpenDatabase();
            var jobs = await db.GenerationJobs.AsNoTracking().Where(j => j.RevisionId == revisionId).ToListAsync();
            Assert.Equal(5, jobs.Count);
            Assert.Equal(JobStatus.Failed, Assert.Single(jobs, j => j.Language == "en").Status);
            Assert.All(jobs.Where(j => j.Language != "en"), job => Assert.Equal(JobStatus.Succeeded, job.Status));
            Assert.NotEmpty(Assert.Single(jobs, j => j.Language == "en").LastError ?? "");

            fixture.Azure.FailedLanguage = null;
            using var retried = await WriteAsync(client, HttpMethod.Post, $"{path}/generate");
            retried.EnsureSuccessStatusCode();
            var final = await db.GenerationJobs.AsNoTracking().Where(j => j.RevisionId == revisionId).ToListAsync();
            Assert.Equal(jobs.Select(j => j.Id).Order(), final.Select(j => j.Id).Order());
            Assert.All(final, job => Assert.Equal(JobStatus.Succeeded, job.Status));
            Assert.Equal(10, observed.Count);
            Assert.All(observed, stage => Assert.True(stage.Persisted == JobStatus.Processing,
                $"{stage.Language}/{stage.Stage}: expected persisted Processing, got {stage.Persisted?.ToString() ?? "missing or wrong stage"}."));
        }
        finally
        {
            fixture.Azure.BeforeSynthesis = null;
            fixture.Azure.BeforeTranslation = null;
            fixture.Azure.FailedLanguage = null;
        }
    }

    private static async Task<Guid> LoginAsync(HttpClient client, string username, string password)
    {
        using var response = await WriteAsync(client, HttpMethod.Post, "/api/auth/login",
            JsonContent.Create(new { username, password }));
        return (await ReadAsync(response)).GetProperty("user").GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreatePlaceAsync(HttpClient client)
    {
        using var response = await WriteAsync(client, HttpMethod.Post, AdminPlaces, PlaceForm("CI place"));
        return (await ReadAsync(response)).GetProperty("id").GetGuid();
    }

    private static MultipartFormDataContent PlaceForm(string title) => new()
    {
        { new StringContent(title), "NameVi" }
    };

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<HttpResponseMessage> WriteAsync(HttpClient client, HttpMethod method, string path,
        HttpContent? body = null)
    {
        using var request = new HttpRequestMessage(method, path) { Content = body };
        using var csrf = await client.GetAsync("/api/auth/csrf");
        request.Headers.Add("X-CSRF-TOKEN", (await ReadAsync(csrf)).GetProperty("token").GetString());
        return await client.SendAsync(request);
    }
}

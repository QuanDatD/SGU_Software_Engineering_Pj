using System.Globalization;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Zoo.Tests.Integration;

public sealed partial class ApiTests
{
    [SqlFact]
    public async Task Br05_ApiAcceptsRadiusGreaterThanZeroAndBelowFive()
    {
        using var client = fixture.CreateClient();
        await LoginAsync(client, "admin", SeedPassword);
        using var response = await WriteAsync(client, HttpMethod.Post, AdminPlaces, PlaceWithRadius("0.5"));
        var placeId = (await ReadAsync(response)).GetProperty("id").GetGuid();
        await using var db = fixture.OpenDatabase();
        Assert.Equal(0.5, (await db.Places.SingleAsync(p => p.Id == placeId)).RadiusMeters);
    }

    [SqlFact]
    public async Task Br05_ApiEnforcesRadiusUpperAndLowerBounds()
    {
        using var client = fixture.CreateClient();
        await LoginAsync(client, "admin", SeedPassword);
        using var zero = await WriteAsync(client, HttpMethod.Post, AdminPlaces, PlaceWithRadius("0"));
        using var tooLarge = await WriteAsync(client, HttpMethod.Post, AdminPlaces, PlaceWithRadius("500.1"));
        using var upperBound = await WriteAsync(client, HttpMethod.Post, AdminPlaces, PlaceWithRadius("500"));
        Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tooLarge.StatusCode);
        Assert.Equal(HttpStatusCode.OK, upperBound.StatusCode);
    }

    [SqlFact]
    public Task Uc05_UpdatingOnlyGpsKeepsRevisionHistory() => AssertPlaceOnlyChangeAsync("Latitude");

    [SqlFact]
    public Task Uc05_UpdatingOnlyRadiusKeepsRevisionHistory() => AssertPlaceOnlyChangeAsync("RadiusMeters");

    [SqlFact]
    public Task Uc05_UpdatingOnlyImageKeepsRevisionHistory() => AssertPlaceOnlyChangeAsync("Image");

    private async Task AssertPlaceOnlyChangeAsync(string field)
    {
        using var client = fixture.CreateClient();
        await LoginAsync(client, "admin", SeedPassword);
        var revisionId = await CreateNarrationAsync(client);
        await GenerateAsync(client, revisionId);
        using var published = await WriteAsync(client, HttpMethod.Post, $"{RevisionPath(revisionId)}/publish");
        published.EnsureSuccessStatusCode();

        await using var db = fixture.OpenDatabase();
        var revision = await db.NarrationRevisions.AsNoTracking().Include(r => r.Place)
            .SingleAsync(r => r.Id == revisionId);
        var vi = await LoadTranslationAsync(db, revisionId, "vi");
        var versions = await db.NarrationRevisions.Where(r => r.PlaceId == revision.PlaceId)
            .Select(r => r.Id).OrderBy(id => id).ToListAsync();
        var newLatitude = field == "Latitude" ? 10.791 : revision.Place.Latitude;
        var newRadius = field == "RadiusMeters" ? 49 : revision.Place.RadiusMeters;
        var form = new MultipartFormDataContent
        {
            { new StringContent(vi.Title), "NameVi" },
            { new StringContent(vi.Description), "DescriptionVi" },
            { new StringContent(newLatitude.ToString(CultureInfo.InvariantCulture)), "Latitude" },
            { new StringContent(newRadius.ToString(CultureInfo.InvariantCulture)), "RadiusMeters" }
        };
        if (field == "Image")
        {
            var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jX1sAAAAASUVORK5CYII=");
            form.Add(new ByteArrayContent(png), "Image", "test.png");
        }

        using var updated = await WriteAsync(client, HttpMethod.Put, $"{AdminPlaces}/{revision.PlaceId}", form);
        updated.EnsureSuccessStatusCode();
        var place = await db.Places.AsNoTracking().SingleAsync(p => p.Id == revision.PlaceId);
        Assert.Equal(newLatitude, place.Latitude);
        Assert.Equal(newRadius, place.RadiusMeters);
        if (field == "Image")
        {
            Assert.StartsWith("/storage/images/", place.ImageUrl);
            using var downloaded = await client.GetAsync(place.ImageUrl);
            downloaded.EnsureSuccessStatusCode();
        }
        Assert.Equal(revisionId, place.PublishedRevisionId);
        Assert.Equal(versions, await db.NarrationRevisions.Where(r => r.PlaceId == place.Id)
            .Select(r => r.Id).OrderBy(id => id).ToListAsync());
    }

    private static MultipartFormDataContent PlaceWithRadius(string radius) => new()
    {
        { new StringContent("CI radius fixture"), "NameVi" },
        { new StringContent(radius), "RadiusMeters" }
    };
}

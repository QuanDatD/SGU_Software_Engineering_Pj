using System.Net;
using Xunit;
using Zoo.DAL.Entities;

namespace Zoo.Tests.Integration;

public sealed partial class ApiTests
{
    [SqlFact]
    public async Task Tc28_InvalidRangeAndEmptyReportHaveDistinctResponses()
    {
        using var client = fixture.CreateClient();
        await LoginAsync(client, "admin", SeedPassword);
        using var invalid = await client.GetAsync("/api/admin/reporting?from=2042-04-11&to=2042-04-11");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        using var empty = await client.GetAsync("/api/admin/reporting?from=2042-04-11&to=2042-04-12");
        var data = await ReadAsync(empty);
        Assert.Equal(0, data.GetProperty("totalVisits").GetInt32());
        Assert.Empty(data.GetProperty("peakHours").EnumerateArray());
        Assert.Equal(24, data.GetProperty("hourBuckets").GetArrayLength());
        Assert.All(data.GetProperty("hourBuckets").EnumerateArray(), bucket =>
            Assert.Equal(0, bucket.GetProperty("count").GetInt32()));
    }

    [SqlFact]
    public async Task Tc29_Tc30_Tc36_ReportingUsesVietnamMidnightAndExcludesDemoByDefault()
    {
        using var client = fixture.CreateClient();
        await LoginAsync(client, "admin", SeedPassword);
        await using var db = fixture.OpenDatabase();
        var midnightUtc = new DateTime(2042, 4, 12, 17, 0, 0, DateTimeKind.Utc);
        db.QrVisits.AddRange(
            Visit(midnightUtc.AddSeconds(-1)),
            Visit(midnightUtc),
            Visit(midnightUtc.AddHours(2)),
            Visit(midnightUtc.AddHours(2).AddMinutes(1), demo: true),
            Visit(midnightUtc.AddDays(1)));
        await db.SaveChangesAsync();

        const string path = "/api/admin/reporting?from=2042-04-13&to=2042-04-14";
        using var operational = await client.GetAsync(path);
        var data = await ReadAsync(operational);
        Assert.Equal(2, data.GetProperty("totalVisits").GetInt32());
        Assert.Equal(new[] { 0, 2 }, data.GetProperty("peakHours").EnumerateArray().Select(h => h.GetInt32()));
        Assert.Equal(24, data.GetProperty("hourBuckets").GetArrayLength());
        Assert.Equal(1, data.GetProperty("hourBuckets")[0].GetProperty("count").GetInt32());
        Assert.Equal(1, data.GetProperty("hourBuckets")[2].GetProperty("count").GetInt32());

        using var inclDemo = await client.GetAsync($"{path}&includeDemo=true");
        var combined = await ReadAsync(inclDemo);
        Assert.Equal(3, combined.GetProperty("totalVisits").GetInt32());
        Assert.Equal(2, Assert.Single(combined.GetProperty("peakHours").EnumerateArray()).GetInt32());
    }

    private static QrVisit Visit(DateTime utc, bool demo = false) => new()
    {
        SessionId = Guid.NewGuid().ToString("N"), VisitedAtUtc = utc, IsDemo = demo
    };
}

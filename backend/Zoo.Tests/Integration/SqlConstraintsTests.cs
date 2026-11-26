using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Zoo.DAL.Entities;

namespace Zoo.Tests.Integration;

[Collection("SQL-backed API")]
[Trait("Category", "SQL")]
public sealed class SqlConstraintsTests(SqlApiFixture fixture)
{
    [SqlFact]
    public async Task Tc18_RawDeleteRejectsPlaceReferencedOnlyByDraft()
    {
        using var client = fixture.CreateClient();
        await using var db = fixture.OpenDatabase();
        var place = new Place { Code = $"CI_{Guid.NewGuid():N}", RadiusMeters = 30 };
        var revision = new NarrationRevision { PlaceId = place.Id, Version = 1 };
        place.Revisions.Add(revision);
        db.Places.Add(place);
        await db.SaveChangesAsync();

        await using var transaction = await db.Database.BeginTransactionAsync();
        var error = await Record.ExceptionAsync(() =>
            db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM dbo.Places WHERE Id = {place.Id}"));
        Assert.Equal(547, Assert.IsType<SqlException>(error).Number);
        Assert.True(await db.Places.AsNoTracking().AnyAsync(p => p.Id == place.Id));
        Assert.True(await db.NarrationRevisions.AsNoTracking().AnyAsync(r => r.Id == revision.Id));
    }

    [SqlFact]
    public async Task Tc04_DatabasePreventsDuplicateQrSession()
    {
        using var client = fixture.CreateClient();
        await using var db = fixture.OpenDatabase();
        var session = Guid.NewGuid().ToString();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO dbo.QrVisits (Id, SessionId, VisitedAtUtc, IsDemo)
            VALUES ({Guid.NewGuid()}, {session}, SYSUTCDATETIME(), 0)
            """);
        var error = await Assert.ThrowsAsync<SqlException>(() =>
            db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO dbo.QrVisits (Id, SessionId, VisitedAtUtc, IsDemo)
                VALUES ({Guid.NewGuid()}, {session}, SYSUTCDATETIME(), 0)
                """));
        Assert.Equal(2601, error.Number);
        Assert.Equal(1, await db.QrVisits.CountAsync(v => v.SessionId == session));
    }
}

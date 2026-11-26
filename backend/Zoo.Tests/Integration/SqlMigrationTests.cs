using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;
using Zoo.DAL.Entities;
using Zoo.DAL.Enums;

namespace Zoo.Tests.Integration;

[Trait("Category", "SQL")]
public sealed class SqlMigrationTests
{
    [SqlFact]
    public async Task AllMigrationsApplyToAnEmptyDatabase()
    {
        await using var database = new SqlApiFixture();
        await using var db = database.OpenDatabase();
        Assert.False(await db.Database.CanConnectAsync());
        var expected = db.Database.GetMigrations().ToArray();
        Assert.NotEmpty(expected);

        await db.Database.MigrateAsync();
        await db.Database.MigrateAsync();

        Assert.Equal(expected, await db.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Empty(await db.Places.AsNoTracking().ToListAsync());
        Assert.Empty(await db.QrVisits.AsNoTracking().ToListAsync());
    }

    [SqlFact]
    public async Task UpgradeFromInitialMigrationPreservesLegacyPublicationAndJob()
    {
        await using var database = new SqlApiFixture();
        await using var db = database.OpenDatabase();
        await db.GetService<IMigrator>().MigrateAsync("20261022113714_InitialCreate");

        var admin = new AdminUser { Username = "legacy-admin", Role = "Admin", PasswordHash = "fixture" };
        var place = new Place { Code = "LEGACY_PLACE" };
        var revision = new NarrationRevision
        {
            PlaceId = place.Id, Version = 1, Status = RevisionStatus.Published,
            PublishedAt = DateTime.UtcNow, CreatedByAdminId = admin.Id
        };
        var translation = new NarrationTranslation { Language = "vi", ScriptText = "Legacy narration" };
        var job = new GenerationJob { Language = "vi", Status = JobStatus.Succeeded, AttemptCount = 2 };
        revision.Translations.Add(translation);
        revision.Jobs.Add(job);
        place.Revisions.Add(revision);

        // The current model can add fields over time, so seed only the columns in the old migration.
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO dbo.AdminUsers (Id, Username, PasswordHash, Role, IsActive, CreatedAt)
            VALUES ({admin.Id}, {admin.Username}, {admin.PasswordHash}, 'Admin', 1, SYSUTCDATETIME());
            INSERT INTO dbo.Places (Id, Code, Category, Latitude, Longitude, RadiusMeters, MapX, MapY,
                Priority, IsActive, CreatedAt, UpdatedAt)
            VALUES ({place.Id}, {place.Code}, 'animal', 10, 106, 20, 0, 0, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
            INSERT INTO dbo.NarrationRevisions (Id, PlaceId, Version, Status, CreatedByAdminId, CreatedAt, PublishedAt)
            VALUES ({revision.Id}, {place.Id}, 1, {(int)revision.Status}, {admin.Id}, SYSUTCDATETIME(), {revision.PublishedAt});
            UPDATE dbo.Places SET PublishedRevisionId = {revision.Id} WHERE Id = {place.Id};
            INSERT INTO dbo.NarrationTranslations (Id, RevisionId, Language, Title, Description, ScriptText,
                ContentHash, AudioStatus, IsEditedByAdmin, UpdatedAt)
            VALUES ({translation.Id}, {revision.Id}, 'vi', 'Legacy', '', {translation.ScriptText}, '', 0, 0, SYSUTCDATETIME());
            INSERT INTO dbo.GenerationJobs (Id, RevisionId, Language, Stage, Status, AttemptCount, CreatedAt, UpdatedAt)
            VALUES ({job.Id}, {revision.Id}, 'vi', 0, {(int)job.Status}, {job.AttemptCount}, SYSUTCDATETIME(), SYSUTCDATETIME());
            """);

        await db.Database.MigrateAsync();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Equal(revision.Id, (await db.Places.SingleAsync(p => p.Id == place.Id)).PublishedRevisionId);
        var upgraded = await db.NarrationRevisions.Include(r => r.Translations).Include(r => r.Jobs)
            .SingleAsync(r => r.Id == revision.Id);
        Assert.Equal(admin.Id, upgraded.CreatedByAdminId);
        Assert.Equal(RevisionStatus.Published, upgraded.Status);
        Assert.Equal(translation.ScriptText, Assert.Single(upgraded.Translations).ScriptText);
        Assert.Equal(job.AttemptCount, Assert.Single(upgraded.Jobs).AttemptCount);
        Assert.Empty(await db.QrVisits.ToListAsync());
    }
}

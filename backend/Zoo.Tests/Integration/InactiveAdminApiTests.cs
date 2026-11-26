using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Xunit;
using Zoo.DAL.Entities;

namespace Zoo.Tests.Integration;

public sealed partial class ApiTests
{
    [SqlFact]
    public async Task Tc16_InactiveAdminWithCorrectPasswordDoesNotReceiveSession()
    {
        using var client = fixture.CreateClient();
        await using var db = fixture.OpenDatabase();
        var inactive = new AdminUser
        {
            Username = $"inactive-{Guid.NewGuid():N}",
            Role = "Admin",
            IsActive = false
        };
        inactive.PasswordHash = new PasswordHasher<AdminUser>().HashPassword(inactive, SeedPassword);
        db.AdminUsers.Add(inactive);
        await db.SaveChangesAsync();

        using var rejected = await WriteAsync(client, HttpMethod.Post, "/api/auth/login",
            JsonContent.Create(new { username = inactive.Username, password = SeedPassword }));
        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
        if (rejected.Headers.TryGetValues("Set-Cookie", out var cookies))
            Assert.DoesNotContain(cookies, cookie => cookie.StartsWith("Zoo.Admin=", StringComparison.Ordinal));

        using var session = await client.GetAsync("/api/auth/me");
        using var adminApi = await client.GetAsync(AdminPlaces);
        Assert.Equal(HttpStatusCode.Unauthorized, session.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, adminApi.StatusCode);
    }
}

using Xunit;
using Zoo.BUS.DTOs;
using Zoo.BUS.Interfaces;
using Zoo.BUS.Services;

namespace Zoo.Tests;

public class GeofenceServiceTests
{
    [Theory]
    [InlineData(91, 0, 10, "INVALID_POSITION")]
    [InlineData(0, 181, 10, "INVALID_POSITION")]
    [InlineData(0, 0, -1, "INVALID_POSITION")]
    [InlineData(0, 0, 26, "LOW_ACCURACY")]
    public async Task EvaluateAsync_RejectsInvalidOrInaccuratePositions(
        double latitude, double longitude, double accuracy, string expectedReason)
    {
        var service = new GeofenceService(new PublishedPlaces([]));

        var result = await service.EvaluateAsync(
            CreatePosition(latitude, longitude, accuracy), TestContext.Current.CancellationToken);

        Assert.False(result.Accepted);
        Assert.Equal(expectedReason, result.ReasonCode);
        Assert.Null(result.PlaceId);
    }

    [Fact]
    public async Task EvaluateAsync_AcceptsPositionOutsideAllZonesWithoutSelectingAPlace()
    {
        var place = CreatePlace(0, 1);
        var service = new GeofenceService(new PublishedPlaces([place]));

        var result = await service.EvaluateAsync(
            CreatePosition(0.001, 0, 10), TestContext.Current.CancellationToken);

        Assert.True(result.Accepted);
        Assert.Equal("OUTSIDE_ZONES", result.ReasonCode);
        Assert.Null(result.PlaceId);
    }

    [Theory]
    [InlineData(1, 2, false)]
    [InlineData(2, 2, true)]
    [InlineData(2, 1, true)]
    public async Task EvaluateAsync_PrefersPriorityThenDistanceForOverlappingZones(
        int nearPriority, int farPriority, bool preferNear)
    {
        var near = CreatePlace(0, nearPriority);
        var far = CreatePlace(0.0002, farPriority);
        var service = new GeofenceService(new PublishedPlaces([near, far]));

        var result = await service.EvaluateAsync(
            CreatePosition(0, 0, 25), TestContext.Current.CancellationToken);

        Assert.True(result.Accepted);
        Assert.Equal("IN_ZONE", result.ReasonCode);
        Assert.Equal((preferNear ? near : far).Id, result.PlaceId);
    }

    private static EvaluateVisitRequest CreatePosition(double latitude, double longitude, double accuracy) =>
        new()
        {
            ResolvedLatitude = latitude,
            ResolvedLongitude = longitude,
            ResolvedAccuracy = accuracy
        };

    private static PlaceDto CreatePlace(double latitude, int priority) =>
        new() { Id = Guid.NewGuid(), Lat = latitude, Lng = 0, RadiusMeters = 50, Priority = priority };

    private sealed class PublishedPlaces(IReadOnlyList<PlaceDto> places) : IPlaceService
    {
        public Task<IReadOnlyList<PlaceDto>> GetPublishedAsync(
            string language, CancellationToken cancellationToken = default) => Task.FromResult(places);

        public Task<PlaceDto?> GetPublishedByIdAsync(
            Guid id, string language, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PlaceDto>> GetAllForAdminAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}

using Zoo.BUS.DTOs;
using Zoo.BUS.Interfaces;

namespace Zoo.BUS.Services;

public sealed class GeofenceService : IGeofenceService
{
    private readonly IPlaceService _placeService;

    // Ngưỡng ban đầu để thử nghiệm.
    private const double MaxAccuracyMeters = 25;
    private const double MaxPositionAgeSeconds = 10;

    public GeofenceService(IPlaceService placeService)
    {
        _placeService = placeService;
    }

    public async Task<EvaluateVisitResponse> EvaluateAsync(
        EvaluateVisitRequest request,
        CancellationToken cancellationToken = default)
    {
        // Bảo vệ trường hợp BUS được gọi ngoài Controller.
        if (request.Latitude is null ||
            request.Longitude is null ||
            request.AccuracyMeters is null ||
            request.RecordedAt is null)
        {
            return Reject("INVALID_POSITION");
        }

        var latitude = request.Latitude.Value;
        var longitude = request.Longitude.Value;
        var accuracy = request.AccuracyMeters.Value;

        if (!double.IsFinite(latitude) ||
            !double.IsFinite(longitude) ||
            !double.IsFinite(accuracy) ||
            latitude < -90 || latitude > 90 ||
            longitude < -180 || longitude > 180 ||
            accuracy < 0)
        {
            return Reject("INVALID_POSITION");
        }

        var ageSeconds =
            (DateTimeOffset.UtcNow - request.RecordedAt.Value)
            .TotalSeconds;

        if (ageSeconds > MaxPositionAgeSeconds)
        {
            return Reject("POSITION_TOO_OLD");
        }

        // Cho phép lệch đồng hồ tối đa 5 giây về phía tương lai.
        if (ageSeconds < -5)
        {
            return Reject("INVALID_TIMESTAMP");
        }

        if (accuracy > MaxAccuracyMeters)
        {
            return Reject("LOW_ACCURACY");
        }

        var places = await _placeService.GetPublishedAsync(
            request.Language,
            cancellationToken);

        var candidates = places
            .Select(place => new
            {
                Place = place,
                Distance = CalculateDistanceMeters(
                    latitude,
                    longitude,
                    place.Latitude,
                    place.Longitude)
            })
            .Where(item =>
                item.Place.RadiusMeters > 0 &&
                accuracy <= item.Place.RadiusMeters &&
                item.Distance <= item.Place.RadiusMeters)
            .OrderByDescending(item => item.Place.Priority)
            .ThenBy(item => item.Distance)
            .ThenBy(item => item.Place.Id)
            .ToList();

        var selected = candidates.FirstOrDefault();

        if (selected is null)
        {
            return new EvaluateVisitResponse
            {
                PositionAccepted = true,
                CandidatePlaceId = null,
                DistanceMeters = null,
                Reason = "NO_ELIGIBLE_ZONE"
            };
        }

        return new EvaluateVisitResponse
        {
            PositionAccepted = true,
            CandidatePlaceId = selected.Place.Id,
            DistanceMeters = Math.Round(selected.Distance, 2),
            Reason = "INSIDE_ZONE"
        };
    }

    private static EvaluateVisitResponse Reject(string reason)
    {
        return new EvaluateVisitResponse
        {
            PositionAccepted = false,
            CandidatePlaceId = null,
            DistanceMeters = null,
            Reason = reason
        };
    }

    private static double CalculateDistanceMeters(
        double lat1,
        double lng1,
        double lat2,
        double lng2)
    {
        const double earthRadiusMeters = 6371000;

        var latitudeDifference = ToRadians(lat2 - lat1);
        var longitudeDifference = ToRadians(lng2 - lng1);

        var a =
            Math.Pow(Math.Sin(latitudeDifference / 2), 2) +
            Math.Cos(ToRadians(lat1)) *
            Math.Cos(ToRadians(lat2)) *
            Math.Pow(Math.Sin(longitudeDifference / 2), 2);

        // Tránh sai số làm giá trị vượt khoảng 0–1.
        a = Math.Clamp(a, 0, 1);

        var c = 2 * Math.Atan2(
            Math.Sqrt(a),
            Math.Sqrt(1 - a));

        return earthRadiusMeters * c;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
}
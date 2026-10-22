using Zoo.BUS.DTOs;
using Zoo.BUS.Interfaces;

namespace Zoo.BUS.Services;

public sealed class GeofenceService : IGeofenceService
{
    private readonly IPlaceService _placeService;
    private const double MaxAccuracyMeters = 100; // Nới lỏng sai số GPS cho thiết bị di động/trình duyệt

    public GeofenceService(IPlaceService placeService)
    {
        _placeService = placeService;
    }

    public async Task<EvaluateVisitResponse> EvaluateAsync(
        EvaluateVisitRequest request,
        CancellationToken cancellationToken = default)
    {
        var lat = request.ResolvedLatitude;
        var lng = request.ResolvedLongitude;
        var acc = request.ResolvedAccuracy;

        if (lat is null || lng is null || acc is null ||
            !double.IsFinite(lat.Value) || !double.IsFinite(lng.Value) ||
            lat < -90 || lat > 90 || lng < -180 || lng > 180)
        {
            return new EvaluateVisitResponse { Accepted = false, ReasonCode = "INVALID_POSITION" };
        }

        if (acc.Value > MaxAccuracyMeters)
        {
            return new EvaluateVisitResponse { Accepted = false, ReasonCode = "LOW_ACCURACY" };
        }

        var places = await _placeService.GetPublishedAsync("vi", cancellationToken);

        PlaceDto? matchedPlace = null;
        double minDistance = double.MaxValue;

        foreach (var place in places)
        {
            var dist = CalculateHaversineMeters(lat.Value, lng.Value, place.Lat, place.Lng);
            if (dist <= place.RadiusMeters)
            {
                if (matchedPlace == null || place.Priority > matchedPlace.Priority ||
                   (place.Priority == matchedPlace.Priority && dist < minDistance))
                {
                    matchedPlace = place;
                    minDistance = dist;
                }
            }
        }

        if (matchedPlace == null)
        {
            return new EvaluateVisitResponse { Accepted = true, ReasonCode = "OUTSIDE_ZONES", PlaceId = null };
        }

        return new EvaluateVisitResponse
        {
            Accepted = true,
            ReasonCode = "IN_ZONE",
            PlaceId = matchedPlace.Id,
            DistanceMeters = Math.Round(minDistance, 1)
        };
    }

    private static double CalculateHaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }
}
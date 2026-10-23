using System.Text.Json.Serialization;

namespace Zoo.BUS.DTOs;

public sealed class EvaluateVisitRequest
{
    [JsonPropertyName("lat")]
    public double? Lat { get; init; }

    [JsonPropertyName("lng")]
    public double? Lng { get; init; }

    [JsonPropertyName("accuracy")]
    public double? Accuracy { get; init; }

    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public double? AccuracyMeters { get; init; }
    public DateTimeOffset? RecordedAt { get; init; }

    public double? ResolvedLatitude => Lat ?? Latitude;
    public double? ResolvedLongitude => Lng ?? Longitude;
    public double? ResolvedAccuracy => Accuracy ?? AccuracyMeters ?? 10;
}

public sealed class EvaluateVisitResponse
{
    public bool Accepted { get; init; }
    public string? ReasonCode { get; init; }
    public Guid? PlaceId { get; init; }
    public double? DistanceMeters { get; init; }
}
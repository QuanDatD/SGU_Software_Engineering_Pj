using System.ComponentModel.DataAnnotations;

namespace Zoo.BUS.DTOs;

// Dữ liệu frontend gửi tới API.
public sealed class EvaluateVisitRequest
{
    [Required]
    [Range(-90d, 90d)]
    public double? Latitude { get; set; }

    [Required]
    [Range(-180d, 180d)]
    public double? Longitude { get; set; }

    [Required]
    [Range(0d, 100000d)]
    public double? AccuracyMeters { get; set; }

    [Required]
    public DateTimeOffset? RecordedAt { get; set; }

    [Required]
    [RegularExpression("^(vi|en|zh|ko|ja)$")]
    public string Language { get; set; } = "vi";
}

// Dữ liệu API trả cho frontend.
public sealed class EvaluateVisitResponse
{
    public bool PositionAccepted { get; init; }

    public Guid? CandidatePlaceId { get; init; }

    public double? DistanceMeters { get; init; }

    public string Reason { get; init; } = "";
}
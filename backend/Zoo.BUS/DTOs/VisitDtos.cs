namespace Zoo.BUS.DTOs;

// ── Geofence ─────────────────────────────────────────────
// Giữ nguyên cấu trúc cũ để GeofenceService không bị ảnh hưởng

public class EvaluateVisitRequest
{
    public double? ResolvedLatitude  { get; set; }
    public double? ResolvedLongitude { get; set; }
    public double? ResolvedAccuracy  { get; set; }

    // Ánh xạ từ JSON { lat, lng, accuracy } mà frontend gửi
    public double? Lat      { set => ResolvedLatitude  = value; }
    public double? Lng      { set => ResolvedLongitude = value; }
    public double? Accuracy { set => ResolvedAccuracy  = value; }
}

public class EvaluateVisitResponse
{
    public bool    Accepted        { get; set; }
    public string? ReasonCode      { get; set; }
    public Guid?   PlaceId         { get; set; }
    public double? DistanceMeters  { get; set; }
}

// ── QR visit recording (UC01.3, BR-20 – BR-26) ───────────
public record RecordQrVisitRequest(string SessionId, bool IsDemo);

public record RecordQrVisitResponse(bool Recorded, string Message);

// ── Reporting (UC07.1 – UC07.3) ──────────────────────────
public record ReportingResponse(
    int TotalVisits,
    HourBucket[] HourBuckets,
    int[] PeakHours          // các giờ đồng hạng (0–23, giờ VN)
);

public record HourBucket(int Hour, int Count);

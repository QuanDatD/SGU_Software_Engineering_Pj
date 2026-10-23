namespace Zoo.BUS.DTOs;

public sealed class PlaceDto
{
    public Guid? RevisionId { get; init; }
    public int? Version { get; init; }
    public Dictionary<string, string> Script { get; init; } = new();
    public Guid Id { get; init; }
    public string Slug { get; init; } = "";
    public string Code { get; init; } = "";
    public string Icon { get; init; } = "📍";
    public string Category { get; init; } = "animal"; // animal | plant | facility

    // Tọa độ GPS thực tế & Bán kính nhận diện
    public double Lat { get; init; }
    public double Lng { get; init; }
    public double RadiusMeters { get; init; }

    // Tọa độ hiển thị trên sơ đồ vẽ (Illustrated Map 1024x643)
    public double MapX { get; init; }
    public double MapY { get; init; }
    public int Priority { get; init; } = 1;
    public bool IsActive { get; init; } = true;
    public string? ImageUrl { get; init; }

    // Đa ngôn ngữ: { "vi": "...", "en": "...", "zh": "...", "ko": "...", "ja": "..." }
    public Dictionary<string, string> Name { get; init; } = new();
    public Dictionary<string, string> Desc { get; init; } = new();
    public Dictionary<string, string?> AudioUrl { get; init; } = new();
}
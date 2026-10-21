namespace Zoo.BUS.DTOs;

public sealed class PlaceDto
{
    public Guid Id { get; init; }

    public string Code { get; init; } = "";
    public string Category { get; init; } = "";
    public string Language { get; init; } = "";

    public string Name { get; init; } = "";
    public string Summary { get; init; } = "";
    public string Script { get; init; } = "";

    public double Latitude { get; init; }
    public double Longitude { get; init; }

    public double RadiusMeters { get; init; }
    public int Priority { get; init; }

    public string? ImageUrl { get; init; }
    public string? AudioUrl { get; init; }
}
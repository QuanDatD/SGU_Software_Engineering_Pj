namespace Zoo.BUS.DTOs;

public class NarrationDto
{
    public Guid PlaceId { get; set; }
    public string PlaceCode { get; set; } = string.Empty;
    public string Language { get; set; } = "vi";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ScriptText { get; set; } = string.Empty;
    public string? AudioUrl { get; set; }
    public bool HasAudio { get; set; }
}

public class GenerationResultDto
{
    public Guid PlaceId { get; set; }
    public int TranslatedCount { get; set; }
    public int SynthesizedAudioCount { get; set; }
    public List<string> ProcessedLanguages { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
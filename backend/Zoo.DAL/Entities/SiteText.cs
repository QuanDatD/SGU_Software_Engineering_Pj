namespace Zoo.DAL.Entities;

public class SiteText
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = null!;
    public string Language { get; set; } = null!;      // vi | en | zh | ko | ja
    public string Value { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
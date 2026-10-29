namespace Zoo.DAL.Integrations.Azure;

public class TranslatorOptions
{
    public string Endpoint { get; set; } = "https://api.cognitive.microsofttranslator.com";
    public string ApiKey { get; set; } = string.Empty;
    public string Region { get; set; } = "southeastasia";
}
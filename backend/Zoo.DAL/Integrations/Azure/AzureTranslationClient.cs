using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Zoo.DAL.Interfaces;

namespace Zoo.DAL.Integrations.Azure;

public class AzureTranslationClient : ITranslationClient
{
    private readonly HttpClient _http;
    private readonly TranslatorOptions _options;

    public AzureTranslationClient(HttpClient http, IOptions<TranslatorOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<string> TranslateAsync(string text, string fromLanguage, string toLanguage, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Nội dung cần dịch không được rỗng.");

        if (fromLanguage.Equals(toLanguage, StringComparison.OrdinalIgnoreCase))
            return text;

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Chưa cấu hình Azure Translator ApiKey.");

        var targetCode = toLanguage == "zh" ? "zh-Hans" : toLanguage;
        var url = $"{_options.Endpoint.TrimEnd('/')}/translate?api-version=3.0&from={fromLanguage}&to={targetCode}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Ocp-Apim-Subscription-Key", _options.ApiKey);
        if (!string.IsNullOrWhiteSpace(_options.Region))
            request.Headers.Add("Ocp-Apim-Subscription-Region", _options.Region);

        request.Content = JsonContent.Create(new[] { new { Text = text } });

        using var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Lỗi Azure Translator ({(int)response.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement[0].GetProperty("translations")[0].GetProperty("text").GetString()
               ?? throw new InvalidOperationException("Azure Translator trả về kết quả rỗng.");
    }
}
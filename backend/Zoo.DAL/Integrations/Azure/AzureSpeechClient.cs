using System.Security;
using System.Text;
using Microsoft.Extensions.Options;
using Zoo.DAL.Interfaces;

namespace Zoo.DAL.Integrations.Azure;

public class AzureSpeechClient : ISpeechClient
{
    private readonly HttpClient _http;
    private readonly SpeechOptions _options;

    private static readonly Dictionary<string, (string Locale, string Voice)> VoiceMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["vi"] = ("vi-VN", "vi-VN-HoaiMyNeural"),
        ["en"] = ("en-US", "en-US-JennyNeural"),
        ["zh"] = ("zh-CN", "zh-CN-XiaoxiaoNeural"),
        ["ko"] = ("ko-KR", "ko-KR-SunHiNeural"),
        ["ja"] = ("ja-JP", "ja-JP-NanamiNeural")
    };

    public AzureSpeechClient(HttpClient http, IOptions<SpeechOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<byte[]> SynthesizeAsync(string text, string language, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Nội dung tạo âm thanh không được rỗng.");

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Chưa cấu hình Azure Speech ApiKey.");

        if (!VoiceMap.TryGetValue(language, out var voiceInfo))
            throw new InvalidOperationException($"Không hỗ trợ giọng đọc cho ngôn ngữ: {language}");

        var endpoint = $"https://{_options.Region}.tts.speech.microsoft.com/cognitiveservices/v1";
        var escapedText = SecurityElement.Escape(text);
        var ssml = $"<speak version='1.0' xml:lang='{voiceInfo.Locale}'><voice name='{voiceInfo.Voice}'>{escapedText}</voice></speak>";

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("Ocp-Apim-Subscription-Key", _options.ApiKey);
        request.Headers.Add("X-Microsoft-OutputFormat", "audio-16khz-128kbitrate-mono-mp3");
        request.Headers.Add("User-Agent", "SaigonZooAudioGuide");
        request.Content = new StringContent(ssml, Encoding.UTF8, "application/ssml+xml");

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Lỗi Azure Speech ({(int)response.StatusCode}): {err}");
        }

        return await response.Content.ReadAsByteArrayAsync(ct);
    }
}
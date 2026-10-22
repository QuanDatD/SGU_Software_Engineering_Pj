using Zoo.BUS.DTOs;
using Zoo.BUS.Interfaces;

namespace Zoo.BUS.Services;

// Chỉ dùng thử trong môi trường Development.
// Chưa đọc database, chưa gọi Azure.
public sealed class DemoPlaceService : IPlaceService
{
    private static readonly Guid ElephantId =
        Guid.Parse("0ca64a71-55b8-41fa-8186-926a672709e2");

    public Task<IReadOnlyList<PlaceDto>> GetPublishedAsync(
        string language,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<PlaceDto> places =
            new[] { CreateDemoPlace(language) };

        return Task.FromResult(places);
    }

    public Task<PlaceDto?> GetPublishedByIdAsync(
        Guid id,
        string language,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        PlaceDto? place = id == ElephantId
            ? CreateDemoPlace(language)
            : null;

        return Task.FromResult(place);
    }

    public Task<IReadOnlyList<PlaceDto>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
        => GetPublishedAsync("vi", cancellationToken);

    private static PlaceDto CreateDemoPlace(string language)
    {
        // Bản dịch mẫu để kiểm tra API, không phải kết quả Azure.
        var content = language switch
        {
            "vi" => (
                Name: "Khu voi",
                Summary: "Điểm tham quan mẫu.",
                Script: "Chào mừng bạn đến với khu voi."
            ),
            "en" => (
                Name: "Elephant Area",
                Summary: "A sample visitor attraction.",
                Script: "Welcome to the elephant area."
            ),
            "zh" => (
                Name: "大象区",
                Summary: "示例参观点。",
                Script: "欢迎来到大象区。"
            ),
            "ko" => (
                Name: "코끼리 구역",
                Summary: "예시 관람 장소입니다.",
                Script: "코끼리 구역에 오신 것을 환영합니다."
            ),
            "ja" => (
                Name: "ゾウエリア",
                Summary: "見学スポットのサンプルです。",
                Script: "ゾウエリアへようこそ。"
            ),
            _ => throw new ArgumentException(
                "Unsupported language.", nameof(language))
        };

        return new PlaceDto
        {
            Id = ElephantId,
            Code = "elephant",
            Category = "animal",


            Name = new() { [language] = content.Name },
            Desc = new() { [language] = content.Summary },
            Script = new() { [language] = content.Script },

            // Tọa độ minh họa, chưa được xác minh thực địa.
            Lat = 10.7882,
            Lng = 106.7042,

            RadiusMeters = 30,
            Priority = 1,

            ImageUrl = null,
            AudioUrl = new()
        };
    }
}
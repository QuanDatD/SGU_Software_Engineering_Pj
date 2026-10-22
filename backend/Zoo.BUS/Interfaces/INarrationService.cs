using Zoo.BUS.DTOs;

namespace Zoo.BUS.Interfaces;

public interface INarrationService
{
    // Lấy bài thuyết minh cho du khách theo ngôn ngữ
    Task<NarrationDto?> GetNarrationAsync(Guid placeId, string language, CancellationToken ct = default);

    // Kích hoạt dịch tự động & sinh audio MP3 cho 1 địa điểm
    Task<GenerationResultDto> GenerateNarrationContentAsync(Guid placeId, bool forceOverwrite = false, CancellationToken ct = default);

    // Xử lý hàng loạt cho toàn bộ các khu đang hoạt động
    Task<List<GenerationResultDto>> GenerateAllPlacesAsync(bool forceOverwrite = false, CancellationToken ct = default);
}
using Zoo.BUS.DTOs;

namespace Zoo.BUS.Interfaces;

public interface INarrationService
{
    // Lấy bài thuyết minh cho du khách theo ngôn ngữ
    Task<NarrationDto?> GetNarrationAsync(Guid placeId, string language, CancellationToken ct = default);

}

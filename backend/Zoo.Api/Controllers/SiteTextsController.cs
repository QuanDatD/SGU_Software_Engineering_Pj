using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zoo.DAL.Data;

namespace Zoo.Api.Controllers;

[ApiController]
[Route("api/site-texts")]
public sealed class SiteTextsController(ZooDbContext db) : ControllerBase
{
    /// <summary>
    /// Trả toàn bộ nhãn giao diện theo ngôn ngữ — BR-28.
    /// GET /api/site-texts?lang=vi
    /// </summary>
    [HttpGet]
    [ResponseCache(Duration = 300)] // cache 5 phút, ít thay đổi
    public async Task<IActionResult> Get([FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var validLangs = new[] { "vi", "en", "zh", "ko", "ja" };
        var target = lang.ToLowerInvariant().Trim();
        if (!validLangs.Contains(target)) target = "vi";

        var texts = await db.SiteTexts
            .AsNoTracking()
            .Where(t => t.Language == target)
            .Select(t => new { t.Key, t.Value })
            .ToListAsync(ct);

        // Trả về dictionary { key: value } để frontend dùng trực tiếp
        var result = texts.ToDictionary(t => t.Key, t => t.Value);
        return Ok(result);
    }
}

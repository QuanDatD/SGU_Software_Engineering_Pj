using Microsoft.AspNetCore.Mvc;
using Zoo.BUS.DTOs;
using Zoo.BUS.Interfaces;

namespace Zoo.Api.Controllers;

[ApiController]
[Route("api/places")]
public sealed class PlacesController : ControllerBase
{
    private static readonly HashSet<string> SupportedLanguages =
        new(StringComparer.Ordinal)
        {
            "vi", "en", "zh", "ko", "ja"
        };

    private readonly IPlaceService _placeService;

    public PlacesController(IPlaceService placeService)
    {
        _placeService = placeService;
    }

    // GET /api/places?lang=vi
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlaceDto>>> GetAll(
        [FromQuery] string? lang = "vi",
        CancellationToken cancellationToken = default)
    {
        var language = NormalizeLanguage(lang);

        if (!SupportedLanguages.Contains(language))
        {
            return InvalidLanguage();
        }

        var places = await _placeService.GetPublishedAsync(
            language, cancellationToken);

        return Ok(places);
    }

    // GET /api/places/{id}?lang=en
    [HttpGet("{id}")]
    public async Task<ActionResult<PlaceDto>> GetById(
        string id,
        [FromQuery] string? lang = "vi",
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var placeId))
        {
            return BadRequest(new
            {
                code = "INVALID_ID",
                message = "ID khu tham quan không hợp lệ."
            });
        }

        var language = NormalizeLanguage(lang);

        if (!SupportedLanguages.Contains(language))
        {
            return InvalidLanguage();
        }

        var place = await _placeService.GetPublishedByIdAsync(
            placeId, language, cancellationToken);

        if (place is null)
        {
            return NotFound(new
            {
                code = "PLACE_NOT_FOUND",
                message = "Không tìm thấy khu tham quan đã xuất bản."
            });
        }

        return Ok(place);
    }

    private static string NormalizeLanguage(string? language)
    {
        return (language ?? "vi").Trim().ToLowerInvariant();
    }

    private BadRequestObjectResult InvalidLanguage()
    {
        return BadRequest(new
        {
            code = "INVALID_LANGUAGE",
            message = "Ngôn ngữ phải là vi, en, zh, ko hoặc ja."
        });
    }
}
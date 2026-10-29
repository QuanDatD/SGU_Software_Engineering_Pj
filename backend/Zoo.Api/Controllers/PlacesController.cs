using Microsoft.AspNetCore.Mvc;
using Zoo.BUS.DTOs;
using Zoo.BUS.Interfaces;

namespace Zoo.Api.Controllers;

[ApiController]
[Route("api/places")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PlacesController : ControllerBase
{
    private readonly IPlaceService _placeService;

    public PlacesController(IPlaceService placeService)
    {
        _placeService = placeService;
    }

    // GET /api/places?lang=vi (Cho User Map)
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlaceDto>>> GetAll(
        [FromQuery] string? lang = "vi",
        CancellationToken cancellationToken = default)
    {
        var places = await _placeService.GetPublishedAsync(lang ?? "vi", cancellationToken);
        return Ok(places);
    }

    // GET /api/places/{id}?lang=vi (Xem chi tiết 1 khu)
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PlaceDto>> GetById(
        Guid id,
        [FromQuery] string? lang = "vi",
        CancellationToken cancellationToken = default)
    {
        var place = await _placeService.GetPublishedByIdAsync(id, lang ?? "vi", cancellationToken);
        if (place is null)
        {
            return NotFound(new { code = "PLACE_NOT_FOUND", message = "Không tìm thấy khu tham quan." });
        }
        return Ok(place);
    }
}
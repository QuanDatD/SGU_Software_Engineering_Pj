using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zoo.BUS.DTOs;
using Zoo.BUS.Interfaces;

namespace Zoo.Api.Controllers;

[ApiController]
[Route("api/admin/places")]
[Authorize(Roles = "Admin")]
public sealed class AdminPlacesController : ControllerBase
{
    private readonly IAdminPlaceService _service;

    public AdminPlacesController(IAdminPlaceService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlaceDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var places = await _service.GetAllAsync(
            cancellationToken);

        return Ok(places);
    }
}
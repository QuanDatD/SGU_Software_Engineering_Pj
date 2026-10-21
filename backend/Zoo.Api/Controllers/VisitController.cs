using Microsoft.AspNetCore.Mvc;
using Zoo.BUS.DTOs;
using Zoo.BUS.Interfaces;

namespace Zoo.Api.Controllers;

[ApiController]
[Route("api/visit")]
public sealed class VisitController : ControllerBase
{
    private readonly IGeofenceService _geofenceService;

    public VisitController(IGeofenceService geofenceService)
    {
        _geofenceService = geofenceService;
    }

    [HttpPost("evaluate")]
    public async Task<ActionResult<EvaluateVisitResponse>> Evaluate(
        [FromBody] EvaluateVisitRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _geofenceService.EvaluateAsync(
            request,
            cancellationToken);

        return Ok(result);
    }
}
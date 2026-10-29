using Microsoft.AspNetCore.Mvc;

namespace Zoo.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "ok",
            service = "Zoo.Api",
            checkedAt = DateTimeOffset.UtcNow
        });
    }
}
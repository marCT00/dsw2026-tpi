using Dsw2026Tpi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;

[ApiController]
[Route("api/doctors/{doctorId:guid}/availabilities")]
[Authorize]
public class DoctorAvailabilityController : ControllerBase
{
    private readonly IAvailabilityService _availabilityService;

    public DoctorAvailabilityController(IAvailabilityService availabilityService)
    {
        _availabilityService = availabilityService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByDoctor(
        [FromRoute] Guid doctorId,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        var result = await _availabilityService.GetSlotsByDoctor(doctorId, year, month);
        return Ok(result);
    }
}

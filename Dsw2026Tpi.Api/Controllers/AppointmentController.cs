using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Dsw2026Tpi.Api.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    [HttpPost]
    [Authorize(Policy = Policies.PatientPolicy)]
    [EnableRateLimiting("BookingPolicy")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] AppointmentModel.Request request)
    {
        var result = await _appointmentService.Create(request);
        return CreatedAtAction(nameof(GetByPatient), new { dni = result.PatientDni }, result);
    }

    [HttpGet("patient")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByPatient([FromQuery] string dni)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole(Roles.Administrator);
        var result = await _appointmentService.GetByPatient(dni, userId, isAdmin);
        return Ok(result);
    }

    [HttpGet("search")]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] int pageSize = 10, [FromQuery] int pageIndex = 1,[FromQuery] string? patientDni = null, [FromQuery] Guid? doctorId = null,[FromQuery] Guid? specialtyId = null,[FromQuery] DateTime? date = null)
    {
        var result = await _appointmentService.Search(pageSize, pageIndex, patientDni, doctorId, specialtyId, date);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.PatientPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel([FromRoute] Guid id)
    {
        await _appointmentService.Cancel(id);
        return Ok();
    }

    [HttpGet]
    [Authorize(Policy = Policies.AdminPolicy)]
    public async Task<IActionResult> GetByDate([FromQuery] DateTime date)
    {
        var result = await _appointmentService.Search(pageSize: 100, pageIndex: 1, patientDni: null, doctorId: null, specialtyId: null, date: date);
        return Ok(result);
    }
}
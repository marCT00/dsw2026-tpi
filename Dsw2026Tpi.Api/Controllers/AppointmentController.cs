using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
    public async Task<IActionResult> Search(
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageIndex = 1,
        [FromQuery] string? dni = null,
        [FromQuery] Guid? doctorId = null,
        [FromQuery] Guid? SpecialityId = null,
        [FromQuery] string? date = null,
        [FromQuery] DateState? status = null)
    {
        var result = await _appointmentService.Search(pageSize, pageIndex, dni, doctorId, SpecialityId, date, status);
        return Ok(result);
    }

    [HttpPatch("{id}/status")]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStatus(
        [FromRoute] Guid id,
        [FromBody] AppointmentModel.UpdateStatusRequest request)
    {
        var result = await _appointmentService.UpdateStatus(id, request);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.PatientPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel([FromRoute] Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole(Roles.Administrator);
        await _appointmentService.Cancel(id, userId, isAdmin);
        return Ok("ok");
    }
}
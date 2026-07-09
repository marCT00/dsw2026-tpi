using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;

[Route("doctors")]
public class DoctorController : AppController
{
    public DoctorController()
    {
        
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery]int pageSize, [FromQuery]int pageIndex, [FromQuery]string? name = null)
    {
        return Ok("Hola Mundo!");
    }
}

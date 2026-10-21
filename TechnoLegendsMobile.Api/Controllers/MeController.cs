using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechnoLegendsMobile.Api.Data;
using TechnoLegendsManagementSystem.Shared.Models;

namespace TechnoLegendsMobile.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MeController(AppDb db) : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        id = User.FindFirst("sub")?.Value,
        email = User.FindFirst("email")?.Value,
        aal = User.FindFirst("aal")?.Value
    });

    [HttpGet("profile")]
    public async Task<ActionResult<ProfileDto>> Profile()
    {
        var id = Guid.Parse(User.FindFirst("sub")!.Value);
        var p = await db.Profiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();

        return new ProfileDto(p.Id, p.FullName, p.Title, p.Department, p.Role,
                              User.FindFirst("email")?.Value ?? "");
    }
}
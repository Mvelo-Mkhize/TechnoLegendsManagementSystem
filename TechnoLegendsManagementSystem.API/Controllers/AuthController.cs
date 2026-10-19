using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TechnoLegendsManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        return Ok(new
        {
            userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            email = User.FindFirst(ClaimTypes.Email)?.Value,
            aal = User.FindFirst("aal")?.Value ?? "aal1"
        });
    }

    [HttpGet("secure")]
    [Authorize]
    public IActionResult SecureEndpoint()
    {
        var aal = User.FindFirst("aal")?.Value;
        if (aal != "aal2")
            return Forbid("MFA required");

        return Ok(new { message = "Access granted to secure resource" });
    }
}
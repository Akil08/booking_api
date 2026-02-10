using appointment_api.DTOs;
using appointment_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace appointment_api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly TokenService _tokenService;

    public AuthController(TokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        if (request == null || request.Id <= 0)
        {
            return BadRequest(new MessageResponse("Invalid id"));
        }

        var role = request.Role?.ToLowerInvariant();
        if (role != "patient" && role != "doctor")
        {
            return BadRequest(new MessageResponse("Invalid role"));
        }

        var token = _tokenService.CreateToken(request.Id, role);
        return Ok(new LoginResponse(token));
    }
}

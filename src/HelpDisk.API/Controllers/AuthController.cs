using HelpDisk.API.Abstractions;
using HelpDisk.Application.Features.Auth;
using HelpDisk.Application.Features.Auth.Dtos;

using Microsoft.AspNetCore.Mvc;
namespace HelpDisk.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ApiController
{
    private readonly IAuthService _authService;
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        return HandleResult(result);
    }
}

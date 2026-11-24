using Microsoft.AspNetCore.Mvc;
using ReShareBlazorApp.Api.Date.Enums;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared;
using ReShareBlazorApp.Shared.DTOs;
using System.Security.Claims;

namespace ReShareBlazorApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly UserManager<User> _userManager;
        private readonly IActivityLogger _activityLogger;

        public AuthController(IAuthService authService, UserManager<User> userManager, IActivityLogger activityLogger)
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);
            if (result == null)
            {
                return BadRequest(new
                {
                    Message = "Nie udało się utworzyć konta"
                });
            }
            return Ok(result);
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            if (result == null)
            {
                await _activityLogger.LogAsync(user.Id, user.UserName, ActivityActions.UserLoginFailed,
                    "Nieudane logowanie", "User", user.Id);
                return Unauthorized(new
                {
                    Message = "Nieprawidłowy email lub hasło."
                });
            }
            await _activityLogger.LogAsync(user.Id, user.UserName, ActivityActions.UserLoggedIn,
                "Zalogowano do serwisu", "User", user.Id);
            return Ok(result);
        }

    }
}

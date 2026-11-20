using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Date.Enums;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly UserManager<User> _userManager;

        public AuthController(IAuthService authService, UserManager<User> userManager)
        {
            _authService = authService;
            _userManager = userManager;
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
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return Unauthorized(new
                {
                    Message = "Nieprawidłowy e-mail lub hasło."
                });
            }

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!isPasswordValid)
            {
                return Unauthorized(new
                {
                    Message = "Nieprawidłowy e-mail lub hasło."
                });
            }

            if (!user.IsActive)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    Message = "Twoje konto zostało trwale zablokowane przez administratora."
                });
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                var localTime = user.LockoutEnd.Value.ToLocalTime();
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    Message = $"Twoje konto zostało tymczasowo zablokowane do {localTime:dd.MM.yyyy HH:mm}."
                });
            }

            var result = await _authService.LoginAsync(request);
            if (result == null)
            {
                return Unauthorized(new
                {
                    Message = "Nieprawidłowy e-mail lub hasło."
                });
            }
            return Ok(result);
        }

    }
}

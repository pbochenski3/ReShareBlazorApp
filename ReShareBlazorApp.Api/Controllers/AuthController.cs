using Microsoft.AspNetCore.Mvc;
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
                return Unauthorized(new
                {
                    Message = "Nieprawidłowy email lub hasło."
                });
            }
            return Ok(result);
        }
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            await _authService.SendPasswordResetAsync(request.Email);
            // Zawsze ta sama odpowiedź - nie zdradzamy, czy konto istnieje.
            return Ok(new
            {
                Message = "Jeśli konto z takim adresem istnieje, wysłaliśmy na nie link do zmiany hasła."
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            var error = await _authService.ResetPasswordAsync(request);
            if (error != null)
            {
                return BadRequest(new { Message = error });
            }
            return Ok(new { Message = "Hasło zostało zmienione." });
        }

    }
}

using Microsoft.AspNetCore.Mvc;
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
                return BadRequest(new RegisterResponse
                {
                    Message = "Nie udało się rozpocząć rejestracji."
                });
            }
            return Ok(result);
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request)
        {
            var result = await _authService.VerifyEmailAsync(request);
            if (result == null)
            {
                return BadRequest(new RegisterResponse
                {
                    Message = "Weryfikacja nie powiodła się."
                });
            }
            return Ok(result);
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmailByLink(
            [FromQuery] string email,
            [FromQuery] string token)
        {
            var result = await _authService.ConfirmEmailByTokenAsync(email, token);
            if (result == null)
            {
                return BadRequest(new RegisterResponse
                {
                    Message = "Potwierdzenie nie powiodło się."
                });
            }
            return Ok(result);
        }

        [HttpPost("resend-code")]
        public async Task<IActionResult> ResendCode(ResendCodeRequest request)
        {
            var result = await _authService.ResendCodeAsync(request.Email);
            if (result == null)
            {
                return BadRequest(new RegisterResponse
                {
                    Message = "Nie udało się wysłać nowego kodu."
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
    }
}

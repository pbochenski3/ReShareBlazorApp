using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ReShareBlazorApp.Api.Date;
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
        {
            _authService = authService;
            _userManager = userManager;
            _activityLogger = activityLogger;
        }

        [HttpPost("verify-registration")]
        public async Task<IActionResult> VerifyRegistration(VerifyRegistrationRequest request)
        {
            var result = await _authService.VerifyRegistrationAsync(request);
            if (result == null)
            {
                return BadRequest(new
                {
                    Message = "Nieprawidłowy lub wygasły kod weryfikacyjny."
                });
            }

            return Ok(result);
        }

        [HttpPost("verify-registration")]
        public async Task<IActionResult> VerifyRegistration(VerifyRegistrationRequest request)
        {
            var result = await _authService.VerifyRegistrationAsync(request);
            if (result == null)
            {
                return BadRequest(new
                {
                    Message = "Nieprawidłowy lub wygasły kod weryfikacyjny."
                });
            }

            return Ok(result);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);
            if (result == null)
            {
                return BadRequest(new RegisterResponse
                {
                    Success = false,
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
                await _activityLogger.LogAsync((int?)null, request.Email, ActivityActions.UserLoginFailed,
                    "Nieudane logowanie (nieznany adres e-mail)", "User", null);
                return Unauthorized(new
                {
                    Message = "Nieprawidłowy e-mail lub hasło."
                });
            }

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!isPasswordValid)
            {
                await _activityLogger.LogAsync(user.Id, user.UserName, ActivityActions.UserLoginFailed,
                    "Nieudane logowanie (nieprawidłowe hasło)", "User", user.Id);
                return Unauthorized(new
                {
                    Message = "Nieprawidłowy e-mail lub hasło."
                });
            }

            if (!user.IsActive)
            {
                await _activityLogger.LogAsync(user.Id, user.UserName, ActivityActions.UserLoginFailed,
                    "Nieudane logowanie (konto zablokowane na stałe)", "User", user.Id);
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    Message = "Twoje konto zostało trwale zablokowane przez administratora."
                });
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                await _activityLogger.LogAsync(user.Id, user.UserName, ActivityActions.UserLoginFailed,
                    "Nieudane logowanie (konto zablokowane czasowo)", "User", user.Id);
                var localTime = user.LockoutEnd.Value.ToLocalTime();
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    Message = $"Twoje konto zostało tymczasowo zablokowane do {localTime:dd.MM.yyyy HH:mm}."
                });
            }

            var result = await _authService.LoginAsync(request);
            if (result == null)
            {
                await _activityLogger.LogAsync(user.Id, user.UserName, ActivityActions.UserLoginFailed,
                    "Nieudane logowanie", "User", user.Id);
                return Unauthorized(new
                {
                    Message = "Nieprawidłowy e-mail lub hasło."
                });
            }
            await _activityLogger.LogAsync(user.Id, user.UserName, ActivityActions.UserLoggedIn,
                "Zalogowano do serwisu", "User", user.Id);
            return Ok(result);
        }
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            int? userId = null;
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedId))
                userId = parsedId;

            await _activityLogger.LogAsync(User, ActivityActions.UserLoggedOut,
                "Wylogowano z serwisu", "User", userId);
            return Ok();
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

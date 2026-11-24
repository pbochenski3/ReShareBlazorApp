using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared;
using ReShareBlazorApp.Shared.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ReShareBlazorApp.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IActivityLogger _activityLogger;
        private readonly IAppEmailSender _emailSender;
        private readonly IBadgeService _badgeService;
        private readonly ILogger<AuthService> _logger;
        private readonly IMemoryCache _cache;

        public AuthService(
            UserManager<User> userManager,
            IConfiguration configuration,
            IActivityLogger activityLogger,
            IAppEmailSender emailSender,
            IBadgeService badgeService,
            ILogger<AuthService> logger,
            IMemoryCache cache)
        {
            _userManager = userManager;
            _configuration = configuration;
            _activityLogger = activityLogger;
            _emailSender = emailSender;
            _badgeService = badgeService;
            _logger = logger;
            _cache = cache;
        }

        public async Task<RegisterResponse?> RegisterAsync(RegisterRequest request)
        {
            if (await _userManager.FindByEmailAsync(request.Email) != null ||
                await _userManager.FindByNameAsync(request.UserName) != null)
            {
                return null;
            }

            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            var cacheKey = GetRegistrationCacheKey(request.Email);

            _cache.Set(cacheKey, new PendingRegistration(request, code), TimeSpan.FromMinutes(10));

            try
            {
                await _emailSender.SendAsync(
                    request.Email,
                    "Kod rejestracji - ODDAJ DALEJ",
                    EmailStyle.RegistrationCode(request.UserName, code));
            }
            catch (Exception exception)
            {
                _cache.Remove(cacheKey);
                _logger.LogError(exception, "Nie udało się wysłać kodu rejestracyjnego do {Email}.", request.Email);
                return null;
            }

            return new RegisterResponse
            {
                Message = "Kod weryfikacyjny został wysłany na podany adres e-mail."
            };
        }

        public async Task<RegisterResponse?> VerifyRegistrationAsync(VerifyRegistrationRequest request)
        {
            var cacheKey = GetRegistrationCacheKey(request.Email);
            if (!_cache.TryGetValue<PendingRegistration>(cacheKey, out var pending) ||
                pending == null ||
                !string.Equals(pending.Code, request.Code.Trim(), StringComparison.Ordinal))
            {
                return null;
            }

            var user = new User
            {
                UserName = pending.Request.UserName,
                Email = pending.Request.Email,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, pending.Request.Password);
            if (!result.Succeeded)
            {
                return null;
            }

            _cache.Remove(cacheKey);

            await _activityLogger.LogAsync(
                user.Id,
                user.UserName ?? "",
                ActivityActions.UserRegistered,
                "Utworzono konto",
                "User",
                user.Id);

            await _badgeService.AwardBadgeAsync(user.Id, "Pierwsze koty za płoty");
            await _badgeService.AwardBadgeAsync(user.Id, "Nowy użytkownik");

            return new RegisterResponse
            {
                Message = "Konto zostało pomyślnie utworzone."
            };
        }

        private static string GetRegistrationCacheKey(string email) =>
            $"registration:{email.Trim().ToUpperInvariant()}";

        private sealed record PendingRegistration(RegisterRequest Request, string Code);

        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive || user.IsDeleted ||
                (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow))
            {
                return null;
            }
            var isPasswordValid = await _userManager.CheckPasswordAsync(
                user,
                request.Password
                );
            if (!isPasswordValid)
            {
                return null;
            }
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
            };
            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)
                );
            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
                );
            var expiresAt = DateTime.Now.AddMinutes(
                Convert.ToDouble(_configuration["Jwt:ExpireMinutes"])
                );
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
                );
            return new LoginResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAt = expiresAt,
                Email = user.Email ?? ""
            };
        }

        public async Task SendPasswordResetAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !user.IsActive || string.IsNullOrEmpty(user.Email))
            {
                return;
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var baseUrl = (_configuration["App:WebBaseUrl"] ?? "https://localhost:7254").TrimEnd('/');
            var link = $"{baseUrl}/reset-password" +
                       $"?email={Uri.EscapeDataString(user.Email)}&token={encodedToken}";
            var body = EmailStyle.ResetPassword(user.UserName, link);

            try
            {
                await _emailSender.SendAsync(user.Email, "Zmiana hasła - ODDAJ DALEJ", body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nie udało się wysłać maila ze zmianą hasła.");
            }
        }

        public async Task<string?> ResetPasswordAsync(ResetPasswordRequest request)
        {
            const string invalidLink =
                "Link do zmiany hasła jest nieprawidłowy lub wygasł. Poproś o nowy.";

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
                return invalidLink;

            string token;
            try
            {
                token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
            }
            catch (FormatException)
            {
                return invalidLink;
            }

            var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
            if (result.Succeeded)
                return null;

            if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken)))
                return invalidLink;

            var messages = result.Errors
                .Select(e => e.Code switch
                {
                    nameof(IdentityErrorDescriber.PasswordTooShort) => "Hasło musi mieć co najmniej 8 znaków.",
                    nameof(IdentityErrorDescriber.PasswordRequiresDigit) => "Hasło musi zawierać cyfrę.",
                    nameof(IdentityErrorDescriber.PasswordRequiresUpper) => "Hasło musi zawierać wielką literę.",
                    nameof(IdentityErrorDescriber.PasswordRequiresLower) => "Hasło musi zawierać małą literę.",
                    nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric) => "Hasło musi zawierać znak specjalny.",
                    _ => "Nie udało się zmienić hasła."
                })
                .Distinct();

            return string.Join(" ", messages);
        }
    }
}
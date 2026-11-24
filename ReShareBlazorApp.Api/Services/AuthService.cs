using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ReShareBlazorApp.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ReshareDbContext _context;
        private readonly ILogger<AuthService> _logger;
        private readonly IBadgeService _badgeService;
        public AuthService(
            UserManager<User> userManager,
            IConfiguration configuration,
            ReshareDbContext context,
            ILogger<AuthService> logger,
            IBadgeService badgeService)
        {
            _userManager = userManager;
            _configuration = configuration;
            _context = context;
            _logger = logger;
            _badgeService = badgeService;
        }
        public async Task<RegisterResponse?> RegisterAsync(RegisterRequest request)
        {
            var user = new User
            {
                UserName = request.UserName,
                Email = request.Email,
                CreatedAt = DateTime.Now,
                IsActive = true
            };
            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                return null;
            }

            try
            {
                var badge = await _context.Badges
                    .FirstOrDefaultAsync(b => b.Name == "Nowy użytkownik");

                if (badge != null)
                {
                    var alreadyEarned = await _context.UserBadges
                        .AnyAsync(ub =>
                            ub.UserId == user.Id &&
                            ub.BadgeId == badge.Id);

            await _badgeService.AwardBadgeAsync(user.Id, "Pierwsze koty za płoty");
                    if (!alreadyEarned)
                    {
                        var userBadge = new UserBadge
                        {
                            UserId = user.Id,
                            BadgeId = badge.Id,
                            IsEarned = true,
                            EarnedAt = DateTime.Now
                        };

                        await _context.UserBadges.AddAsync(userBadge);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "Nie znaleziono odznaki Nowy użytkownik.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Nie udało się przyznać odznaki nowemu użytkownikowi.");
            }

            return new RegisterResponse
            {
                Message = "Konto zostało pomyślnie utworzone."
            };
        }
        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
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
                new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
                new Claim(ClaimTypes.Name,user.UserName ?? ""),
                new Claim(ClaimTypes.Email,user.Email ?? ""),
            };
            var userRoles = await _userManager.GetRolesAsync(user);
            foreach(var role in userRoles)
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
    }
}

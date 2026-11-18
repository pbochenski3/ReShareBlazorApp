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
        private readonly ReshareDbContext _dbContext;
        private readonly IEmailService _emailService;

        public AuthService(
            UserManager<User> userManager,
            IConfiguration configuration,
            ReshareDbContext dbContext,
            IEmailService emailService)
        {
            _userManager = userManager;
            _configuration = configuration;
            _dbContext = dbContext;
            _emailService = emailService;
        }

        public async Task<RegisterResponse?> RegisterAsync(RegisterRequest request)
        {
            // Sprawdź czy użytkownik o takim emailu lub nazwie już istnieje
            var existingByEmail = await _userManager.FindByEmailAsync(request.Email);
            if (existingByEmail != null)
            {
                return new RegisterResponse
                {
                    Message = "Konto z tym adresem e-mail już istnieje."
                };
            }

            var existingByName = await _userManager.FindByNameAsync(request.UserName);
            if (existingByName != null)
            {
                return new RegisterResponse
                {
                    Message = "Ta nazwa użytkownika jest już zajęta."
                };
            }

            // Walidacja nazwy użytkownika przez Identity (dozwolone znaki itp.)
            var tempUserForValidation = new User { UserName = request.UserName, Email = request.Email };
            if (_userManager.UserValidators != null)
            {
                foreach (var validator in _userManager.UserValidators)
                {
                    var validationResult = await validator.ValidateAsync(_userManager, tempUserForValidation);
                    if (!validationResult.Succeeded)
                    {
                        var errors = string.Join(" ", validationResult.Errors.Select(e => e.Description));
                        return new RegisterResponse
                        {
                            Message = $"Nieprawidłowa nazwa użytkownika: {errors}"
                        };
                    }
                }
            }

            // Walidacja hasła przez Identity
            if (_userManager.PasswordValidators != null)
            {
                foreach (var validator in _userManager.PasswordValidators)
                {
                    var validationResult = await validator.ValidateAsync(_userManager, tempUserForValidation, request.Password);
                    if (!validationResult.Succeeded)
                    {
                        var errors = string.Join(" ", validationResult.Errors.Select(e => e.Description));
                        return new RegisterResponse
                        {
                            Message = $"Nieprawidłowe hasło: {errors}"
                        };
                    }
                }
            }

            // Usuń poprzednie oczekujące rejestracje dla tego emaila
            var oldPending = await _dbContext.PendingRegistrations
                .Where(p => p.Email == request.Email)
                .ToListAsync();
            if (oldPending.Any())
            {
                _dbContext.PendingRegistrations.RemoveRange(oldPending);
            }

            // Generuj kod 6-cyfrowy i token
            var code = GenerateCode();
            var token = Guid.NewGuid().ToString("N");

            // Hashuj hasło tymczasowo (używając Identity hasher)
            var tempUser = new User();
            var passwordHash = _userManager.PasswordHasher.HashPassword(tempUser, request.Password);

            var pending = new PendingRegistration
            {
                UserName = request.UserName,
                Email = request.Email,
                PasswordHash = passwordHash,
                VerificationCode = code,
                VerificationToken = token,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };

            _dbContext.PendingRegistrations.Add(pending);
            await _dbContext.SaveChangesAsync();

            // Wyślij e-mail z kodem i linkiem
            await _emailService.SendVerificationEmailAsync(request.Email, code, token);

            return new RegisterResponse
            {
                Message = "Wysłaliśmy kod weryfikacyjny na Twój adres e-mail. Sprawdź skrzynkę i wpisz kod lub kliknij link w wiadomości.",
                RequiresEmailConfirmation = true,
                Email = request.Email
            };
        }

        public async Task<RegisterResponse?> VerifyEmailAsync(VerifyEmailRequest request)
        {
            var pending = await _dbContext.PendingRegistrations
                .Where(p => p.Email == request.Email && p.VerificationCode == request.Code)
                .FirstOrDefaultAsync();

            if (pending == null)
            {
                return new RegisterResponse
                {
                    Message = "Nieprawidłowy kod weryfikacyjny."
                };
            }

            if (pending.ExpiresAt < DateTime.UtcNow)
            {
                _dbContext.PendingRegistrations.Remove(pending);
                await _dbContext.SaveChangesAsync();
                return new RegisterResponse
                {
                    Message = "Kod weryfikacyjny wygasł. Spróbuj zarejestrować się ponownie."
                };
            }

            return await CreateUserFromPending(pending);
        }

        public async Task<RegisterResponse?> ConfirmEmailByTokenAsync(string email, string token)
        {
            var pending = await _dbContext.PendingRegistrations
                .Where(p => p.Email == email && p.VerificationToken == token)
                .FirstOrDefaultAsync();

            if (pending == null)
            {
                return new RegisterResponse
                {
                    Message = "Nieprawidłowy lub wygasły link potwierdzający."
                };
            }

            if (pending.ExpiresAt < DateTime.UtcNow)
            {
                _dbContext.PendingRegistrations.Remove(pending);
                await _dbContext.SaveChangesAsync();
                return new RegisterResponse
                {
                    Message = "Link potwierdzający wygasł. Spróbuj zarejestrować się ponownie."
                };
            }

            return await CreateUserFromPending(pending);
        }

        public async Task<RegisterResponse?> ResendCodeAsync(string email)
        {
            var pending = await _dbContext.PendingRegistrations
                .Where(p => p.Email == email)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (pending == null)
            {
                return new RegisterResponse
                {
                    Message = "Nie znaleziono oczekującej rejestracji dla tego adresu e-mail."
                };
            }

            // Wygeneruj nowy kod i token
            pending.VerificationCode = GenerateCode();
            pending.VerificationToken = Guid.NewGuid().ToString("N");
            pending.ExpiresAt = DateTime.UtcNow.AddMinutes(15);

            await _dbContext.SaveChangesAsync();

            await _emailService.SendVerificationEmailAsync(pending.Email, pending.VerificationCode, pending.VerificationToken);

            return new RegisterResponse
            {
                Message = "Nowy kod weryfikacyjny został wysłany na Twój adres e-mail.",
                RequiresEmailConfirmation = true,
                Email = email
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
            var jwtToken = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
                );
            return new LoginResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                ExpiresAt = expiresAt,
                Email = user.Email ?? ""
            };
        }

        private async Task<RegisterResponse> CreateUserFromPending(PendingRegistration pending)
        {
            // Sprawdź ponownie unikalność (ochrona przed race condition)
            var existingByEmail = await _userManager.FindByEmailAsync(pending.Email);
            if (existingByEmail != null)
            {
                _dbContext.PendingRegistrations.Remove(pending);
                await _dbContext.SaveChangesAsync();
                return new RegisterResponse
                {
                    Message = "Konto z tym adresem e-mail już istnieje."
                };
            }

            var user = new User
            {
                UserName = pending.UserName,
                Email = pending.Email,
                EmailConfirmed = true,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            // Tworzymy użytkownika z już zahashowanym hasłem
            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                return new RegisterResponse
                {
                    Message = "Nie udało się utworzyć konta: " +
                              string.Join(", ", result.Errors.Select(e => e.Description))
                };
            }

            // Ustawiamy zahashowane hasło bezpośrednio
            user.PasswordHash = pending.PasswordHash;
            await _userManager.UpdateAsync(user);

            // Usuń oczekującą rejestrację
            _dbContext.PendingRegistrations.Remove(pending);
            await _dbContext.SaveChangesAsync();

            return new RegisterResponse
            {
                Message = "Konto zostało pomyślnie utworzone! Możesz się teraz zalogować."
            };
        }

        private static string GenerateCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}

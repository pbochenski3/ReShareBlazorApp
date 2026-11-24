using Microsoft.AspNetCore.Identity;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Interfaces.Repositories;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Services
{
    public class UserService : IUserService
    {
        private const long MaxAvatarBytes = 2 * 1024 * 1024;
        private const string AvatarFolder = "avatars";
        private readonly IUserRepository _userRepository;
        private readonly UserManager<User> _userManager;
        private readonly IImageStorage _imageStorage;

        public UserService(
            IUserRepository userRepository,
            UserManager<User> userManager,
            IImageStorage imageStorage)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _imageStorage = imageStorage;
        }

        public async Task<bool> CheckIfUserExistAsync(string username)
        {
            return await _userRepository.CheckIfUserExistAsync(username);
        }

        public async Task<UserProfileDTO?> GetPublicProfileAsync(string username)
        {
            var user = await _userRepository.GetUserByUserNameAsync(username);
            return user == null ? null : ToDto(user, includeEmail: false);
        }

        public async Task<UserProfileDTO?> GetMyProfileAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            return user == null ? null : ToDto(user, includeEmail: true);
        }

        public async Task<string?> UpdateProfileAsync(int userId, UpdateProfileRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return "Nie znaleziono użytkownika.";

            user.City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim();
            user.Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim();

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded ? null : "Nie udało się zapisać zmian.";
        }

        public async Task<string?> SetAvatarAsync(int userId, Stream content, string contentType, long length)
        {
            if (length <= 0 || length > MaxAvatarBytes)
                return "Zdjęcie jest za duże (maks. 2 MB).";

            using var ms = new MemoryStream();
            await content.CopyToAsync(ms);
            var bytes = ms.ToArray();

            // Rozszerzenie wynika z zawartości pliku (nie ufamy nazwie ani Content-Type).
            var ext = DetectImageExtension(bytes);
            if (ext == null)
                return "Dozwolone formaty: JPG, PNG, WebP.";

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return "Nie znaleziono użytkownika.";

            var fileName = await _imageStorage.SaveAsync(bytes, ext, AvatarFolder);

            var oldFile = user.AvatarPath;
            user.AvatarPath = fileName;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                _imageStorage.Delete(fileName, AvatarFolder);
                return "Nie udało się zapisać zdjęcia.";
            }
            DeleteFile(oldFile);
            return null;
        }

        public async Task<string?> RemoveAvatarAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return "Nie znaleziono użytkownika.";

            var oldFile = user.AvatarPath;
            user.AvatarPath = null;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) return "Nie udało się usunąć zdjęcia.";
            DeleteFile(oldFile);
            return null;
        }

        private UserProfileDTO ToDto(User user, bool includeEmail) => new()
        {
            UserId = user.Id,
            UserName = user.UserName ?? "",
            Email = includeEmail ? user.Email : null,
            City = user.City,
            Bio = user.Bio,
            AvatarUrl = string.IsNullOrEmpty(user.AvatarPath) ? null : $"/avatars/{user.AvatarPath}",
            CreatedAt = user.CreatedAt
        };

        private void DeleteFile(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;
            // Path.GetFileName chroni przed wyjściem poza folder Avatars.
            _imageStorage.Delete(Path.GetFileName(fileName), AvatarFolder);
        }

        private static string? DetectImageExtension(byte[] b)
        {
            if (b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return ".jpg";
            if (b.Length > 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return ".png";
            if (b.Length > 12 && b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F'
                && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P') return ".webp";
            return null;
        }
    }
}

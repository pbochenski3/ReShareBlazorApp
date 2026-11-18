using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Interfaces.Services
{
    public interface IUserService
    {
        Task<bool> CheckIfUserExistAsync(string username);
        Task<UserProfileDTO?> GetPublicProfileAsync(string username);
        Task<UserProfileDTO?> GetMyProfileAsync(int userId);
        // Zwraca null gdy OK, w przeciwnym razie komunikat błędu.
        Task<string?> UpdateProfileAsync(int userId, UpdateProfileRequest request);
        Task<string?> SetAvatarAsync(int userId, Stream content, string contentType, long length);
        Task<string?> RemoveAvatarAsync(int userId);
    }
}

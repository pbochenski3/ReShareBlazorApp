using ReShareBlazorApp.Api.Date;

namespace ReShareBlazorApp.Api.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<bool> CheckIfUserExistAsync(string username);
        Task<User> GetUserByIdAsync(int userId);
    }
}

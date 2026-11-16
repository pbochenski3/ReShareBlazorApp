using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Interfaces.Services
{
    public interface IUserService
    {
        Task<bool> CheckIfUserExistAsync(string username);
    }
}

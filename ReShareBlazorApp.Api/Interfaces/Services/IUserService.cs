namespace ReShareBlazorApp.Api.Interfaces.Services
{
    public interface IUserService
    {
        Task<bool> CheckIfUserExistAsync(string username);
    }
}

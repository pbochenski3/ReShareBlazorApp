namespace ReShareBlazorApp.Api.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<bool> CheckIfUserExistAsync(string username);
    }
}

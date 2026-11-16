using Microsoft.Identity.Client;
using ReShareBlazorApp.Api.Interfaces.Repositories;
using ReShareBlazorApp.Api.Interfaces.Services;

namespace ReShareBlazorApp.Api.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }
        public async Task<bool> CheckIfUserExistAsync(string username)
        {
            return await _userRepository.CheckIfUserExistAsync(username);
        }
    }
}

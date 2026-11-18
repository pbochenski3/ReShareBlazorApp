using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Interfaces.Repositories;

namespace ReShareBlazorApp.Api.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ReshareDbContext _context;
        private readonly ILogger<ProductRepository> _logger;
        public UserRepository(ReshareDbContext context, ILogger<ProductRepository> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<bool> CheckIfUserExistAsync(string username)
        {
            try
            {
                return await _context.Users.AnyAsync(u => u.UserName.Equals(username));
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"[UserRepository]CheckIfUserExistAsync {ex}");
                return false;
            }
        }
        public async Task<User?> GetUserByUserNameAsync(string username)
        {
            try
            {
                return await _context.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserName == username && u.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"[UserRepository]GetUserByUserNameAsync {ex}");
                throw;
            }
        }
        public async Task<User> GetUserByIdAsync(int userId)
        {
            try
            {
                return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"[UserRepository]CheckIfUserExistAsync {ex}");
                throw;
            }
        }
    }
}

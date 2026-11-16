using Microsoft.AspNetCore.Mvc;
using ReShareBlazorApp.Api.Interfaces.Services;

namespace ReShareBlazorApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }
        [HttpGet]
        public async Task<ActionResult<bool>> CheckIfUserExistAsync
            ([FromQuery] string username)
        {
            return await _userService.CheckIfUserExistAsync(username);
        }

    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared;
using System.Security.Claims;

namespace ReShareBlazorApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ReshareDbContext _context;
        private readonly IActivityLogger _activityLogger;

        public UsersController(ReshareDbContext context, IActivityLogger activityLogger)
        {
            _context = context;
            _activityLogger = activityLogger;
        }

        // GET: api/users/{targetUserId}/is-following
        [HttpGet("{targetUserId}/is-following")]
        [Authorize]
        public async Task<IActionResult> IsFollowing(int targetUserId)
        {
            var currentUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserIdClaim == null) return Unauthorized();

            var currentUserId = int.Parse(currentUserIdClaim);
            var exists = await _context.UserFollows
                .AnyAsync(f => f.FollowerId == currentUserId && f.FollowingId == targetUserId);
			if (!int.TryParse(currentUserIdClaim, out var currentUserId))
				return Unauthorized();
			var exists = await _context.UserFollows
				.AnyAsync(f => f.FollowerId == currentUserId && f.FollowingId == targetUserId);

            return Ok(exists);
        }

        // POST: api/users/{targetUserId}/toggle-follow
        [HttpPost("{targetUserId}/toggle-follow")]
        [Authorize]
        public async Task<IActionResult> ToggleFollow(int targetUserId)
        {
            var currentUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserIdClaim == null) return Unauthorized();

            var currentUserId = int.Parse(currentUserIdClaim);
			if (!int.TryParse(currentUserIdClaim, out var currentUserId))
				return Unauthorized();

            if (currentUserId == targetUserId)
                return BadRequest("Nie możesz obserwować samego siebie.");

            var follow = await _context.UserFollows
                .FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FollowingId == targetUserId);

            if (follow == null)
            {
                _context.UserFollows.Add(new UserFollow { FollowerId = currentUserId, FollowingId = targetUserId });
                await _context.SaveChangesAsync();
                var followedName = await _context.Users.Where(u => u.Id == targetUserId)
                    .Select(u => u.UserName).FirstOrDefaultAsync();
                await _activityLogger.LogAsync(User, ActivityActions.UserFollowed,
                    $"Zaczął obserwować użytkownika {followedName} (id: {targetUserId})", "User", targetUserId);
                return Ok(new { isFollowing = true, message = "Zaczęto obserwować użytkownika." });
            }
            else
            {
                _context.UserFollows.Remove(follow);
                await _context.SaveChangesAsync();
                var unfollowedName = await _context.Users.Where(u => u.Id == targetUserId)
                    .Select(u => u.UserName).FirstOrDefaultAsync();
                await _activityLogger.LogAsync(User, ActivityActions.UserUnfollowed,
                    $"Przestał obserwować użytkownika {unfollowedName} (id: {targetUserId})", "User", targetUserId);
                return Ok(new { isFollowing = false, message = "Przestałoby się obserwować użytkownika." });
            }
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Controllers;

[ApiController]
public class LockdownController : ControllerBase
{
    private readonly ReshareDbContext _db;   // nazwa Twojego DbContext
    private readonly IActivityLogger _activityLogger;

    public LockdownController(ReshareDbContext db, IActivityLogger activityLogger)
    {
        _db = db;
        _activityLogger = activityLogger;
    }

    // Publiczny – z niego korzysta strona, żeby wiedzieć czy pokazać przerwę techniczną.
    [HttpGet("api/lockdown")]
    [AllowAnonymous]
    public async Task<LockdownDTO> Get()
    {
        var settings = await _db.LockdownSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == 1);

        return new LockdownDTO
        {
            IsLocked = settings?.IsLocked ?? false
        };
    }

    // Tylko admin – zmienia wartość w bazie.
    [HttpPut("api/admin/lockdown")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Set([FromBody] LockdownDTO dto)
    {
        var settings = await _db.LockdownSettings
            .SingleOrDefaultAsync(x => x.Id == 1);

        if (settings == null)
        {
            settings = new LockdownSettings
            {
                IsLocked = dto.IsLocked
            };

            await _db.LockdownSettings.AddAsync(settings);
        }
        else
        {
            settings.IsLocked = dto.IsLocked;
        }

        await _db.SaveChangesAsync();

        await _activityLogger.LogAsync(User, ActivityActions.AdminLockdownChanged,
            dto.IsLocked ? "Włączono przerwę techniczną" : "Wyłączono przerwę techniczną",
            "Lockdown", 1);

        return NoContent();
    }
}
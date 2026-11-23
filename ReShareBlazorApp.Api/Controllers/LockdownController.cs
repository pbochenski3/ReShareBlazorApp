using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Controllers;

[ApiController]
public class LockdownController : ControllerBase
{
    private readonly ReshareDbContext _db;   // nazwa Twojego DbContext
    public LockdownController(ReshareDbContext db) => _db = db;

    // Publiczny – z niego korzysta strona, żeby wiedzieć czy pokazać przerwę techniczną.
    [HttpGet("api/lockdown")]
    [AllowAnonymous]
    public async Task<LockdownDTO> Get()
    {
        var rows = await _db.Database
            .SqlQueryRaw<bool>("SELECT IsLocked AS Value FROM LockdownSettings WHERE Id = 1")
            .ToListAsync();

        return new LockdownDTO { IsLocked = rows.FirstOrDefault() };
    }

    // Tylko admin – zmienia wartość w bazie.
    [HttpPut("api/admin/lockdown")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Set([FromBody] LockdownDTO dto)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE LockdownSettings SET IsLocked = {dto.IsLocked} WHERE Id = 1");

        return NoContent();
    }
}
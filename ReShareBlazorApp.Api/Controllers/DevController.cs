using Microsoft.AspNetCore.Mvc;
using ReShareBlazorApp.Api.Date;

namespace ReShareBlazorApp.Api.Controllers
{
    // Narzędzie do testów: otwórz w przeglądarce https://localhost:7266/api/dev/seed
    // Dodaje przykładowe ogłoszenia i pokazuje, ile ich jest w bazie albo jaki jest błąd.
    [ApiController]
    [Route("api/dev")]
    public class DevController : ControllerBase
    {
        private readonly IServiceProvider _services;
        private readonly IWebHostEnvironment _env;
        public DevController(IServiceProvider services, IWebHostEnvironment env)
        {
            _services = services;
            _env = env;
        }

        [HttpGet("seed")]
        public async Task<IActionResult> Seed()
        {
            if (!_env.IsDevelopment())
                return NotFound(); // tylko lokalnie
            var (added, total, error) = await DbSeeder.SeedAsync(_services);
            return Ok(new { added, total, error });
        }
    }
}

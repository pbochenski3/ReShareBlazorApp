using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ReShareBlazorApp.Api.Date
{
    public class ReshareDbContext : IdentityDbContext<User,IdentityRole<int>,int>
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public ReshareDbContext(DbContextOptions<ReshareDbContext> options)
            : base(options)
        {
            
        }
    }
}

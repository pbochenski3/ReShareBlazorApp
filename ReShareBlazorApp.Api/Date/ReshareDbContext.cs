using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ReShareBlazorApp.Api.Date;

namespace ReShareBlazorApp.Api.Date
{
    public class ReshareDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Review> Reviews { get; set; } // Dodana tabela opinii

        public ReshareDbContext(DbContextOptions<ReshareDbContext> options)
            : base(options)
        {

        }
    }
}
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
        public DbSet<Favorite> Favorites { get; set; } // nowa tabela z ulubionymi
        public ReshareDbContext(DbContextOptions<ReshareDbContext> options)
            : base(options)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // najpierw konfiguracja Identity

            builder.Entity<Favorite>(entity =>
            {
                // jeden użytkownik może dodać dane ogłoszenie tylko raz
                entity.HasKey(f => new { f.UserId, f.ProductId });

                // NoAction, bo SQL Server nie pozwala na dwie ścieżki kaskadowego usuwania
                // (User -> Product -> Favorite oraz User -> Favorite)
                entity.HasOne(f => f.User)
                      .WithMany()
                      .HasForeignKey(f => f.UserId)
                      .OnDelete(DeleteBehavior.NoAction);

                // usunięcie ogłoszenia usuwa też wpisy z ulubionych
                entity.HasOne(f => f.Product)
                      .WithMany()
                      .HasForeignKey(f => f.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}

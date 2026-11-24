using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ReShareBlazorApp.Api.Date
{


    public class ReshareDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<UserFollow> UserFollows { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<FavoriteProduct> FavoriteProducts { get; set; }

        public ReshareDbContext(DbContextOptions<ReshareDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<UserFollow>()
                .HasKey(f => new { f.FollowerId, f.FollowingId });

            builder.Entity<Product>()
                .HasQueryFilter(p => !p.isDeleted);

            builder.Entity<Report>()
                .HasOne(r => r.Product)
                .WithMany()
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Report>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ulubione: jeden wiersz = jedna para (użytkownik, ogłoszenie).
            // Usunięcie ogłoszenia kasuje je z ulubionych; User = Restrict (jak w Report),
            // bo SQL Server nie pozwala na kilka ścieżek kaskadowego usuwania.
            builder.Entity<FavoriteProduct>()
                .HasKey(f => new { f.UserId, f.ProductId });

            builder.Entity<FavoriteProduct>()
                .HasOne(f => f.Product)
                .WithMany()
                .HasForeignKey(f => f.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<FavoriteProduct>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
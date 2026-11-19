using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ReShareBlazorApp.Api.Date
{
	// Tabela przechowująca relacje obserwowania (kto -> kogo) w stylu Vinted
	public class UserFollow
	{
		public int FollowerId { get; set; } // Kto obserwuje
		public int FollowingId { get; set; } // Kogo obserwują
	}

	public class ReshareDbContext : IdentityDbContext<User, IdentityRole<int>, int>
	{
		public DbSet<User> Users { get; set; }
		public DbSet<Product> Products { get; set; }
		public DbSet<Location> Locations { get; set; }
		public DbSet<UserFollow> UserFollows { get; set; }

		public ReshareDbContext(DbContextOptions<ReshareDbContext> options)
			: base(options)
		{
		}

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			// Klucz złożony zapobiegający duplikatom obserwacji
			builder.Entity<UserFollow>()
				.HasKey(f => new { f.FollowerId, f.FollowingId });
		}
	}
}
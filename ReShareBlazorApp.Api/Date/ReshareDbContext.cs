
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ReShareBlazorApp.Api.Date
{
	public class ReshareDbContext
		: IdentityDbContext<User, IdentityRole<int>, int>
	{
		public DbSet<User> Users { get; set; }
		public DbSet<Product> Products { get; set; }
		public DbSet<Location> Locations { get; set; }

		public DbSet<RecentlyViewedProduct>
			RecentlyViewedProducts
		{ get; set; }

		public ReshareDbContext(
			DbContextOptions<ReshareDbContext> options)
			: base(options)
		{
		}

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			builder.Entity<RecentlyViewedProduct>(entity =>
			{
				entity.HasKey(x => x.Id);

				// Ten sam produkt może wystąpić tylko raz
				// w historii danego użytkownika.
				entity.HasIndex(x => new
				{
					x.UserId,
					x.ProductId
				}).IsUnique();

				// Przyspiesza pobieranie ostatnio oglądanych.
				entity.HasIndex(x => new
				{
					x.UserId,
					x.ViewedAt
				});

				entity.Property(x => x.ViewedAt)
					.HasColumnType("datetime2");

				entity.HasOne(x => x.User)
					.WithMany()
					.HasForeignKey(x => x.UserId)
					.OnDelete(DeleteBehavior.Cascade);

				entity.HasOne(x => x.Product)
				.WithMany()
				.HasForeignKey(x => x.ProductId)
				.OnDelete(DeleteBehavior.NoAction);
			});
		}
	}
}

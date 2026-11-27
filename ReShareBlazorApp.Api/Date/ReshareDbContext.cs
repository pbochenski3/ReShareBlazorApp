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
        public DbSet<RecentlyViewedProduct> RecentlyViewedProducts { get; set; }
        public DbSet<PromoBanner> PromoBanners { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }
        public DbSet<LockdownSettings> LockdownSettings { get; set; }
        public DbSet<Badge> Badges { get; set; }
        public DbSet<UserBadge> UserBadges { get; set; }
        public DbSet<FavoriteProduct> FavoriteProducts { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<ChatConversation> ChatConversations { get; set; }
        public DbSet<ChatParticipant> ChatParticipants { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<ChatAuditLog> ChatAuditLogs { get; set; }



        public ReshareDbContext(DbContextOptions<ReshareDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ActivityLog>(entity =>
            {
                entity.Property(x => x.CreatedAt).HasColumnType("datetime2");
                // Szukanie po użytkowniku i sortowanie po dacie.
                entity.HasIndex(x => x.UserName);
                entity.HasIndex(x => x.UserId);
                entity.HasIndex(x => x.CreatedAt);
            });

            builder.Entity<UserFollow>()
                .HasKey(f => new { f.FollowerId, f.FollowingId });
            builder.Entity<UserFollow>()
                .HasKey(f => new { f.FollowerId, f.FollowingId });

            builder.Entity<Product>()
                .HasQueryFilter(p => !p.isDeleted);

            builder.Entity<User>()
                .HasQueryFilter(u => !u.IsDeleted);

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

            builder.Entity<RecentlyViewedProduct>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => new
                {
                    x.UserId,
                    x.ProductId
                }).IsUnique();

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

            builder.Entity<PromoBanner>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Message).HasMaxLength(250).IsRequired();
                entity.Property(x => x.BadgeText).HasMaxLength(40);
                entity.Property(x => x.LinkUrl).HasMaxLength(300);
                entity.Property(x => x.LinkText).HasMaxLength(60);
                entity.Property(x => x.CreatedAt).HasColumnType("datetime2");
                entity.Property(x => x.ExpiresAt).HasColumnType("datetime2");
            });

            // Użytkownik może zdobyć daną odznakę tylko raz.
            builder.Entity<UserBadge>()
                .HasIndex(ub => new { ub.UserId, ub.BadgeId })
                .IsUnique();

            builder.Entity<FavoriteProduct>(entity =>
            {
                entity.HasKey(f => new { f.UserId, f.ProductId });

                entity.HasOne(f => f.User)
                    .WithMany()
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(f => f.Product)
                    .WithMany()
                    .HasForeignKey(f => f.ProductId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.Property(f => f.AddedAt)
                    .HasColumnType("datetime2");

                builder.Entity<Review>()
                    .HasIndex(r => new { r.ProductId, r.UserId })
                    .IsUnique()
                    .HasFilter("[UserId] IS NOT NULL");

                builder.Entity<Review>()
                    .HasOne<User>()
                    .WithMany()
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                builder.Entity<Payment>(entity =>
                {
                    entity.HasKey(p => p.Id);

                    entity.Property(p => p.ProductName)
                        .HasMaxLength(200)
                        .IsRequired();

                    entity.Property(p => p.Currency)
                        .HasMaxLength(3)
                        .IsRequired();

                    entity.Property(p => p.StripeSessionId)
                        .HasMaxLength(255);

                    entity.Property(p => p.StripeIdempotencyKey)
                        .HasMaxLength(255);

                    entity.Property(p => p.StripePaymentIntentId)
                        .HasMaxLength(255);

                    entity.Property(p => p.CreatedAt)
                        .HasColumnType("datetime2");

                    entity.Property(p => p.PaidAt)
                        .HasColumnType("datetime2");

                    entity.HasIndex(p => p.StripeSessionId)
                        .IsUnique();

                    entity.HasIndex(p => p.UserId);

                    entity.HasIndex(p => p.ProductId);

                    entity.HasOne(p => p.User)
                        .WithMany()
                        .HasForeignKey(p => p.UserId)
                        .OnDelete(DeleteBehavior.NoAction);

                    entity.HasOne(p => p.Product)
                        .WithMany()
                        .HasForeignKey(p => p.ProductId)
                        .OnDelete(DeleteBehavior.NoAction);
                });
            });

            // CHAT - konfiguracja encji

            builder.Entity<ChatConversation>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.CreatedAt).HasColumnType("datetime2");
                entity.Property(x => x.LastMessageAt).HasColumnType("datetime2");

                entity.Property(x => x.SupportReference)
                    .HasMaxLength(80);

                entity.HasMany(x => x.Participants)
                    .WithOne(x => x.Conversation)
                    .HasForeignKey(x => x.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(x => x.Messages)
                    .WithOne(x => x.Conversation)
                    .HasForeignKey(x => x.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ChatParticipant>(entity =>
            {
                // Klucz złożony - jeden użytkownik tylko raz w rozmowie
                entity.HasKey(x => new { x.ConversationId, x.UserId });

                entity.Property(x => x.JoinedAt).HasColumnType("datetime2");
                entity.Property(x => x.LastReadAt).HasColumnType("datetime2");

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(x => x.UserId);
            });

            builder.Entity<ChatMessage>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.CipherText).IsRequired();
                entity.Property(x => x.Nonce).HasMaxLength(32).IsRequired();
                entity.Property(x => x.AuthTag).HasMaxLength(32).IsRequired();
                entity.Property(x => x.CreatedAt).HasColumnType("datetime2");

                entity.HasOne(x => x.Sender)
                    .WithMany()
                    .HasForeignKey(x => x.SenderId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(x => x.ReplyToMessage)
                    .WithMany()
                    .HasForeignKey(x => x.ReplyToMessageId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(x => new { x.ConversationId, x.CreatedAt });
                entity.HasIndex(x => x.SenderId);
            });

            builder.Entity<ChatAuditLog>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.EventType)
                    .HasMaxLength(40)
                    .IsRequired();

                entity.Property(x => x.CipherText).IsRequired();
                entity.Property(x => x.Nonce).HasMaxLength(32).IsRequired();
                entity.Property(x => x.AuthTag).HasMaxLength(32).IsRequired();
                entity.Property(x => x.CreatedAt).HasColumnType("datetime2");

                entity.HasIndex(x => new { x.ConversationId, x.CreatedAt });
            });

        }
    }
}
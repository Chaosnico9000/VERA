using Microsoft.EntityFrameworkCore;

namespace VERA.Server.Data
{
    public class VeraDbContext : DbContext
    {
        public VeraDbContext(DbContextOptions<VeraDbContext> options) : base(options) { }

        public DbSet<User>         Users         => Set<User>();
        public DbSet<TimeEntry>    TimeEntries   => Set<TimeEntry>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<User>(e =>
            {
                e.HasIndex(u => u.Username).IsUnique();
                e.Property(u => u.Username).HasMaxLength(50);
            });

            b.Entity<TimeEntry>(e =>
            {
                e.HasKey(t => t.Id);
                e.HasOne(t => t.User)
                 .WithMany(u => u.TimeEntries)
                 .HasForeignKey(t => t.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(t => t.UserId);
            });

            b.Entity<RefreshToken>(e =>
            {
                e.HasOne(r => r.User)
                 .WithMany(u => u.RefreshTokens)
                 .HasForeignKey(r => r.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(r => r.Token).IsUnique();
            });
        }
    }
}

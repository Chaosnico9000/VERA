using System.ComponentModel.DataAnnotations;

namespace VERA.Server.Data
{
    public class User
    {
        public int      Id             { get; set; }
        [MaxLength(50)]
        public string   Username       { get; set; } = string.Empty;
        public string   PasswordSalt   { get; set; } = string.Empty;
        public string   PasswordHash   { get; set; } = string.Empty;
        public DateTime CreatedAt      { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt   { get; set; }
        public int      FailedAttempts { get; set; }
        public DateTime? LockedUntil   { get; set; }

        public ICollection<TimeEntry>    TimeEntries    { get; set; } = [];
        public ICollection<RefreshToken> RefreshTokens  { get; set; } = [];
    }

    public class TimeEntry
    {
        public Guid     Id        { get; set; } = Guid.NewGuid();
        public int      UserId    { get; set; }
        public User     User      { get; set; } = null!;
        [MaxLength(200)]
        public string   Title     { get; set; } = string.Empty;
        [MaxLength(100)]
        public string   Category  { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime  { get; set; }
        public int      Type      { get; set; }  // EntryType enum value
    }

    public class RefreshToken
    {
        public int      Id         { get; set; }
        public int      UserId     { get; set; }
        public User     User       { get; set; } = null!;
        public string   Token      { get; set; } = string.Empty;
        public DateTime ExpiresAt  { get; set; }
        public bool     Revoked    { get; set; }
        public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
        public string   CreatedByIp { get; set; } = string.Empty;
    }
}

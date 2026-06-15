namespace RequestHub.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }          // who logged in/out
        public string Action { get; set; } = string.Empty; // "Login" or "Logout"
        public string? IpAddress { get; set; }   // IP address
        public string? UserAgent { get; set; }   // browser info
        public bool IsSuccess { get; set; } = true; // was login successful
        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
    }
}
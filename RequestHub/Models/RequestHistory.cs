namespace RequestHub.Models
{
    public class RequestHistory
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public string Action { get; set; } = string.Empty;    // e.g. "Created", "StatusChanged", "CommentAdded"
        public string? OldStatus { get; set; }
        public string? NewStatus { get; set; }
        public int PerformedById { get; set; }
        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

        // Security logging
        public string? IpAddress { get; set; }                // IP address of the user
        public string? UserAgent { get; set; }                // browser/client info
    }
}
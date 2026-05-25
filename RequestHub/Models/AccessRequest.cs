namespace RequestHub.Models
{
    public class AccessRequest
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;  // e.g. Projects folder
        public string AccessType { get; set; } = string.Empty; // Read, Write, Full, Admin
        public string Justification { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;    // Draft, Submitted, InApproval, Approved, Rejected
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Basis { get; set; }                    // HR ticket / document reference
        public string? Department { get; set; }               // user's department
        public string Priority { get; set; } = "Medium";      // Low, Medium, High
        public DateTime? ExpiryDate { get; set; }             // access expiry date (optional)

        // User acknowledgement after access is granted
        public bool IsAcknowledged { get; set; } = false;     // whether user confirmed access
        public DateTime? AcknowledgedAt { get; set; }         // when user acknowledged
        public int? AcknowledgedBy { get; set; }              // who acknowledged
    }
}
namespace RequestHub.Models
{
    public class AccessRequest
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public string AccessType { get; set; } = string.Empty; // Read, Write, Full, Admin
        public string Justification { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Draft, Submitted, InApproval, Approved, Rejected
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? Basis { get; set; }
        public string? Department { get; set; }          // user's department
        public string Priority { get; set; } = "Medium"; // Low, Medium, High
        public DateTime? ExpiryDate { get; set; }        // access expiry date (optional)
    }
}
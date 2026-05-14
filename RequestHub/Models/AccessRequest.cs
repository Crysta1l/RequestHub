namespace RequestHub.Models
{
    public class AccessRequest
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty; // e.g Projects folder
        public string AccessType { get; set; } = string.Empty; // Read, Write, Full,
        public string Justification { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty; // Draft, Submitted, InApproval, Approved, Rejected, Completed
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

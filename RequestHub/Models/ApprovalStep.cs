namespace RequestHub.Models
{
    public class ApprovalStep
    {
        // Who approved
        public int Id { get; set; }
        public int RequestId { get; set; }     // Which request this belongs to
        public int ApprovedId { get; set; }   // User who must approve
        public int Order { get; set; }        // 1, 2, 3  sequence
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public string? Comment { get; set; }
        public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;
    }
}

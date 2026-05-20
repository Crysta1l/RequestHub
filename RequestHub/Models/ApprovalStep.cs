namespace RequestHub.Models
{
    public class ApprovalStep
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public int Order { get; set; }        // 1 = Approver, 2 = Admin
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public string? Comment { get; set; }
        public int? ApproverId { get; set; }  // filled when approved
        public DateTime? ApprovedAt { get; set; }

        public AccessRequest Request { get; set; } = null!;
        public User? Approver { get; set; }
    }
}
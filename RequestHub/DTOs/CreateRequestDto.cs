namespace RequestHub.DTOs
{
    public class CreateRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public string AccessType { get; set; } = string.Empty;
        public string Justification { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium"; // Low, Medium, High
        public string? Department { get; set; }          // user's department
        public DateTime? ExpiryDate { get; set; }        // access expiry date
        public string? Basis { get; set; }
    }
}
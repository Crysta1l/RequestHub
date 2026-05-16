namespace RequestHub.DTOs
{
    public class CreateRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public string AccessType { get; set; } = string.Empty;
        public string Justification { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium";
    }
}

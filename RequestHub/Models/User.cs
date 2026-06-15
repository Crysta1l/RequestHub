namespace RequestHub.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string HashPassword { get; set; } = string.Empty;
        public string Role { get; set; } = "Requester";
        public bool IsActive { get; set; } = true;        // for soft delete
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // registration date
    }
}
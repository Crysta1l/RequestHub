namespace RequestHub.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string HashPassword { get; set; } = string.Empty;
        public string Role { get; set; } = "Requested";
    }
}

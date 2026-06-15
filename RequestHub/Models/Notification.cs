namespace RequestHub.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }           // who receives notification
        public string Message { get; set; } = string.Empty; // notification text
        public int? RequestId { get; set; }       // related request (optional)
        public bool IsRead { get; set; } = false; // read status
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
        public AccessRequest? Request { get; set; }
    }
}
namespace RequestHub.Models
{
    public class RequestFile
    {
        public int Id { get; set; }
        public int RequestId { get; set; }           // which request this file belongs to
        public string FileName { get; set; } = string.Empty;    // original file name
        public string StoredName { get; set; } = string.Empty;  // name on disk (guid)
        public string ContentType { get; set; } = string.Empty; // e.g. application/pdf
        public long FileSize { get; set; }           // bytes
        public int UploadedBy { get; set; }          // user id
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public AccessRequest Request { get; set; } = null!;
    }
}
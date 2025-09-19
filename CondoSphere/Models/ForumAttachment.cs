namespace CondoSphere.Models
{
    public class ForumAttachment
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public ForumPost? Post { get; set; }

        public string FileName { get; set; } = "";
        public string Path { get; set; } = "";           // /uploads/forum/xxxx.ext (web path)
        public string ContentType { get; set; } = "application/octet-stream";
        public long Size { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

}

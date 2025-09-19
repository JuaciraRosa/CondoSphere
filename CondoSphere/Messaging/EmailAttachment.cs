namespace CondoSphere.Messaging
{
    public class EmailAttachment
    {
        public string FileName { get; init; } = "";
        public string ContentType { get; init; } = "application/octet-stream";
        public byte[] Content { get; init; } = Array.Empty<byte>();
    }
}

namespace CondoSphere.Models
{
    public class StaffChatAlert
    {
        public int Id { get; set; }
        public string ResidentId { get; set; } = string.Empty;
        public string MessagePreview { get; set; } = string.Empty; // até ~120 chars
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? SeenAtUtc { get; set; }                 // preenchido quando staff abre o chat
        public int? CondominiumId { get; set; }                   // opcional, se quiser escopo por condomínio
    }
}

using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public class Announcement
    {
        public int Id { get; set; }

        [Required, StringLength(140)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(8000)]
        public string Body { get; set; } = string.Empty;

        // Escopo do comunicado
        [Display(Name = "Condominium")]
        public int? CondominiumId { get; set; }
        [ValidateNever] public Condominium? Condominium { get; set; }

        // Entrega
        public bool SendEmail { get; set; } = true;
        public bool SendInApp { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ScheduledAtUtc { get; set; } // null = enviar agora

        // Métricas simples
        public int EmailsQueued { get; set; }
        public int InAppDelivered { get; set; }

        // (Opcional) anexo simples
        [StringLength(400)]
        public string? AttachmentPath { get; set; }
    }
}

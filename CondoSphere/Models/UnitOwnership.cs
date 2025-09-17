using CondoSphere.Data;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public class UnitOwnership
    {
        public int Id { get; set; }

        [Required]
        public int UnitId { get; set; }

        [ValidateNever]
        public Unit? Unit { get; set; }

        // dono (User) — pode ser nulo (unidade “vazia”)
        public string? OwnerId { get; set; }

        [ValidateNever]
        public User? Owner { get; set; }

        public DateTimeOffset StartAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? EndAt { get; set; }  // null = atual
    }
}

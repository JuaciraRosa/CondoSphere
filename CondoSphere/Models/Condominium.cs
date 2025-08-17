using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public class Condominium
    {
        public int Id { get; set; }
        [Required]
        [StringLength(100, ErrorMessage = "Name can't exceed 100 characters.")]
        public string Name { get; set; }

        [Required]
        [StringLength(200, ErrorMessage = "Address can't exceed 200 characters.")]
        public string Address { get; set; }

        [Required]
        public Company Company { get; set; }

        public ICollection<Unit> Units { get; set; }
        public ICollection<Meeting> Meetings { get; set; }
        public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; }
        public ICollection<Expense> Expenses { get; set; }
        public ICollection<Notification> Notifications { get; set; }
    }

}

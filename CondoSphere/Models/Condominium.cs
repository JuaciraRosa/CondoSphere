using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
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


        // FK -> Company
        [Required]
        public int CompanyId { get; set; }

        [ValidateNever]
        public Company? Company { get; set; }


        [ValidateNever]
        public ICollection<Unit> Units { get; set; }

        [ValidateNever]
        public ICollection<Meeting> Meetings { get; set; }

        [ValidateNever]
        public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; }

        [ValidateNever]
        public ICollection<Expense> Expenses { get; set; }

        [ValidateNever]
        public ICollection<Notification> Notifications { get; set; }
    }

}

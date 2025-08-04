namespace CondoSphere.Models
{
    public class Condominium
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }

        public int CompanyId { get; set; }
        public Company Company { get; set; }

        public ICollection<Unit> Units { get; set; }
        public ICollection<Meeting> Meetings { get; set; }
        public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; }
        public ICollection<Expense> Expenses { get; set; }
        public ICollection<Notification> Notifications { get; set; }
    }

}

using CondoSphere.Data;

namespace CondoSphere.Models
{
    public class MaintenanceRequest
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime SubmittedAt { get; set; }
        public RequestStatus Status { get; set; }

        public int CondominiumId { get; set; }
        public Condominium Condominium { get; set; }

        public int SubmittedById { get; set; }
        public User SubmittedBy { get; set; }
    }

}

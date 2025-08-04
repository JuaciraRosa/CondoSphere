namespace CondoSphere.Models
{
    public class Meeting
    {
        public int Id { get; set; }
        public DateTime ScheduledDate { get; set; }
        public string Agenda { get; set; }
        public string MinutesDocumentPath { get; set; }

        public int CondominiumId { get; set; }
        public Condominium Condominium { get; set; }
    }

}

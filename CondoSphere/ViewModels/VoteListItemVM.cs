namespace CondoSphere.ViewModels
{
    public class VoteListItemVM
    {
        public int MeetingId { get; set; }
        public DateTime ScheduledDate { get; set; }
        public string Agenda { get; set; } = string.Empty;
        public string Condominium { get; set; } = string.Empty;
        public bool AlreadyVoted { get; set; }
    }
}

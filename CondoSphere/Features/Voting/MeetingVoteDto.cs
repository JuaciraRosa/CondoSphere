namespace CondoSphere.Features.Voting
{
    public class MeetingVoteDto
    {
        public int MeetingId { get; set; }
        public string UnitNumber { get; set; } = "";
        public string VoterEmail { get; set; } = "";
        public string Choice { get; set; } = ""; // "A favor", "Contra", "Abstenção"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

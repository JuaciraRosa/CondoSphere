namespace CondoSphere.Features.Voting
{
    public class MeetingVoteDto
    {
        public string Id { get; init; } = Guid.NewGuid().ToString(); // identificador estável
        public int MeetingId { get; set; }
        public string UnitNumber { get; set; } = "";
        public string VoterEmail { get; set; } = "";
        public string Choice { get; set; } = ""; // "A favor", "Contra", "Abstenção"
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    }

}

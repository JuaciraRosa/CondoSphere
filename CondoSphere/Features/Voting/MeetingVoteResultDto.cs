namespace CondoSphere.Features.Voting
{
    public class MeetingVoteResultDto
    {
        public int MeetingId { get; set; }
        public int Total { get; set; }
        public int AFavor { get; set; }
        public int Contra { get; set; }
        public int Abstencao { get; set; }
    }
}

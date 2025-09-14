using CondoSphere.Features.Voting;
using CondoSphere.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CondoSphere.Data.Interfaces
{
    public interface IVotingRepository
    {
        Task<IReadOnlyList<VoteListItemVM>> ListMeetingsForUserAsync(string userId, string userEmail);
        Task<IReadOnlyList<SelectListItem>> GetUnitsForMeetingAsync(int meetingId);
        Task<bool> HasUserVotedAsync(int meetingId, string voterEmail);
        Task UpsertVoteAsync(MeetingVoteDto vote);
        Task<MeetingVoteResultDto> GetResultAsync(int meetingId);
        Task<IReadOnlyList<MeetingVoteDto>> GetVotesByMeetingAsync(int meetingId);
    }
}

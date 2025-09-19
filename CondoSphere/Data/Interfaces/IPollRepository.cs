using CondoSphere.Models;

namespace CondoSphere.Data.Interfaces
{
    public interface IPollRepository
    {
        Task<Poll?> GetByIdAsync(int id);
        Task<Poll?> GetDetailsAsync(int id);
        Task<IEnumerable<Poll>> GetOpenByCondoAsync(int condominiumId);
        Task<IEnumerable<Poll>> GetByCreatorAsync(string userId, int? condominiumId = null);

        Task AddAsync(Poll poll);
        Task CloseAsync(int pollId);
        Task DeleteAsync(int pollId);

        Task<PollVote> UpsertVoteAsync(int pollId, int optionId, string userId);

        Task UpdateAsync(Poll poll, IEnumerable<string> newOptionsToAppend);

        Task<IEnumerable<Poll>> GetOpenForUserAsync(string userId);
        Task<bool> HasUserVotedAsync(int pollId, string userId);

        Task<IEnumerable<Poll>> GetAllWithCondoAsync(int? condominiumId = null);
    }
}

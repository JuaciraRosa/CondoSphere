using CondoSphere.Models;

namespace CondoSphere.Data.Interfaces
{
    public interface IChatAlertRepository
    {
        Task AddAsync(StaffChatAlert alert);
        Task<int> CountUnseenAsync(DateTime sinceUtc, int? condominiumId = null);
        Task MarkAllSeenAsync(int? condominiumId = null);
    }
}

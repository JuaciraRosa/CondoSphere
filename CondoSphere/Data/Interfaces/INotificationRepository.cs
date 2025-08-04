using CondoSphere.Models;

namespace CondoSphere.Data.Interfaces
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {
        Task<IEnumerable<Notification>> GetRecentAsync(int condominiumId, int count);
    }

}

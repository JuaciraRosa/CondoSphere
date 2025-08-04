using CondoSphere.Models;

namespace CondoSphere.Data.Interfaces
{
    public interface IMeetingRepository : IGenericRepository<Meeting>
    {
        Task<IEnumerable<Meeting>> GetUpcomingAsync(int condominiumId);
    }

}

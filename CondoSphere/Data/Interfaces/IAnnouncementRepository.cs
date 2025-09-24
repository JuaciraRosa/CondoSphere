using CondoSphere.Models;

namespace CondoSphere.Data.Interfaces
{
    public interface IAnnouncementRepository : IGenericRepository<Announcement>
    {
        Task<IEnumerable<Announcement>> GetLatestAsync(int? condominiumId, int take = 100);

      
    }
}

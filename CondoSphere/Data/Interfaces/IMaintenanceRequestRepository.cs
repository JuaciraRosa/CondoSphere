using CondoSphere.Models;

namespace CondoSphere.Data.Interfaces
{
    public interface IMaintenanceRequestRepository : IGenericRepository<MaintenanceRequest>
    {
        Task<IEnumerable<MaintenanceRequest>> GetOpenRequestsAsync();
    }

}

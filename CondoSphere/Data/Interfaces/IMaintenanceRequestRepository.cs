using CondoSphere.Models;

namespace CondoSphere.Data.Interfaces
{
    public interface IMaintenanceRequestRepository : IGenericRepository<MaintenanceRequest>
    {
        Task<IEnumerable<MaintenanceRequest>> GetOpenRequestsAsync();
        Task<IEnumerable<MaintenanceRequest>> GetAllDetailedAsync();     // Include Condominium + SubmittedBy
        Task<MaintenanceRequest?> GetByIdDetailedAsync(int id);
    }

}

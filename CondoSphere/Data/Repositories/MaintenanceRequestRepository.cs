using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Data.Repositories
{
    public class MaintenanceRequestRepository : GenericRepository<MaintenanceRequest>, IMaintenanceRequestRepository
    {
        public MaintenanceRequestRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<MaintenanceRequest>> GetOpenRequestsAsync()
        {
            return await _context.MaintenanceRequests
                .Where(r => r.Status == RequestStatus.Open)
                .ToListAsync();
        }
    }

}

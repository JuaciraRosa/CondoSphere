using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Data.Repositories
{
    public class MaintenanceRequestRepository : GenericRepository<MaintenanceRequest>, IMaintenanceRequestRepository
    {
        public MaintenanceRequestRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<MaintenanceRequest>> GetOpenRequestsAsync()
      => await _context.MaintenanceRequests
          .AsNoTracking()
          .Include(m => m.Condominium)
          .Include(m => m.SubmittedBy)
          .Where(m => m.Status == RequestStatus.Open)
          .OrderByDescending(m => m.SubmittedAt)
          .ToListAsync();

        public async Task<IEnumerable<MaintenanceRequest>> GetAllDetailedAsync()
      => await _context.MaintenanceRequests
          .AsNoTracking()
          .Include(m => m.Condominium)
          .Include(m => m.SubmittedBy)
          .OrderByDescending(m => m.SubmittedAt)
          .ToListAsync();

        public async Task<MaintenanceRequest?> GetByIdDetailedAsync(int id)
            => await _context.MaintenanceRequests
                .Include(m => m.Condominium)
                .Include(m => m.SubmittedBy)
                .FirstOrDefaultAsync(m => m.Id == id);
    }

}

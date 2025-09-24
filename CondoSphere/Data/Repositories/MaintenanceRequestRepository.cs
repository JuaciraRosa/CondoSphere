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
            .AsNoTracking() 
            .Include(m => m.Condominium)
            .Include(m => m.SubmittedBy)
            .FirstOrDefaultAsync(m => m.Id == id);


        public async Task<string?> GetRequesterEmailAsync(string userId)
        {
            // Ajuste o DbSet se o seu tipo for ApplicationUser, etc.
            return await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();
        }


        public async Task<int> CountOpenAsync()
    => await _context.MaintenanceRequests
        .AsNoTracking()
        .CountAsync(r => r.Status == RequestStatus.Open || r.Status == RequestStatus.InProgress);

    }

}

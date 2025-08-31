using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Data.Repositories
{
    public class MeetingRepository : GenericRepository<Meeting>, IMeetingRepository
    {
        public MeetingRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Meeting>> GetAllWithCondoAsync()
        {
            return await _context.Meetings
                .Include(m => m.Condominium)
                .ThenInclude(c => c.Company)            // <= carrega a empresa
                .OrderByDescending(m => m.ScheduledDate)
                .ToListAsync();
        }

        public async Task<Meeting?> GetByIdWithCondoAsync(int id)
        {
            return await _context.Meetings
                .Include(m => m.Condominium)
                .ThenInclude(c => c.Company)            // <= carrega a empresa
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<IEnumerable<Meeting>> GetUpcomingAsync(int condominiumId)
        {
            return await _context.Meetings
                .Where(m => m.CondominiumId == condominiumId && m.ScheduledDate >= DateTime.Now)
                .OrderBy(m => m.ScheduledDate)
                .ToListAsync();
        }
    }

}

using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Data.Repositories
{
    public class MeetingRepository : GenericRepository<Meeting>, IMeetingRepository
    {
        public MeetingRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Meeting>> GetUpcomingAsync(int condominiumId)
        {
            return await _context.Meetings
                .Where(m => m.CondominiumId == condominiumId && m.ScheduledDate >= DateTime.Now)
                .OrderBy(m => m.ScheduledDate)
                .ToListAsync();
        }
    }

}

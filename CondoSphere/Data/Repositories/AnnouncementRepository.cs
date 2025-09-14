using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Data.Repositories
{
    public class AnnouncementRepository : GenericRepository<Announcement>, IAnnouncementRepository
    {
        public AnnouncementRepository(ApplicationDbContext ctx) : base(ctx) { }

        public async Task<IEnumerable<Announcement>> GetLatestAsync(int? condoId, int take = 100)
        {
            IQueryable<Announcement> q = _context.Announcements
                .AsNoTracking()
                .Include(a => a.Condominium);

            if (condoId.HasValue)
                q = q.Where(a => a.CondominiumId == condoId.Value || a.CondominiumId == null);

            return await q
                .OrderByDescending(a => a.CreatedAt)
                .Take(take)
                .ToListAsync();
        }
    }
}

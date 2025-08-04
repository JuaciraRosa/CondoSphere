using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Data.Repositories
{
    public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Notification>> GetRecentAsync(int condominiumId, int count)
        {
            return await _context.Notifications
                .Where(n => n.CondominiumId == condominiumId)
                .OrderByDescending(n => n.SentAt)
                .Take(count)
                .ToListAsync();
        }
    }

}

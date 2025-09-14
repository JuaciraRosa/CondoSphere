using CondoSphere.Data;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Services
{
    public class AnnouncementBadgeService : IAnnouncementBadgeService
    {
        private readonly ApplicationDbContext _db;
        public AnnouncementBadgeService(ApplicationDbContext db) => _db = db;

        public async Task<int> CountRecentAsync(string userId, int days = 3)
        {
            var now = DateTime.UtcNow;
            var since = now.AddDays(-days);

            // condomínios do utilizador (pode ter mais de uma unidade)
            var myCondoIds = await _db.Units
                .Where(u => u.OwnerId == userId)
                .Select(u => u.CondominiumId)
                .Distinct()
                .ToListAsync();

            // publicado = chegou a hora (ou envio imediato)
            // recente   = (ScheduledAtUtc ?? CreatedAt) >= since
            return await _db.Announcements
                .Where(a => (a.ScheduledAtUtc == null || a.ScheduledAtUtc <= now))
                .Where(a => (a.ScheduledAtUtc ?? a.CreatedAt) >= since)
                .Where(a => !a.CondominiumId.HasValue || myCondoIds.Contains(a.CondominiumId.Value))
                .CountAsync();
        }
    }
}

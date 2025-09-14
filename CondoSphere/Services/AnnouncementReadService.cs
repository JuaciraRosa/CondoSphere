using CondoSphere.Data;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Services
{
    public class AnnouncementReadService : IAnnouncementReadService
    {
        private readonly ApplicationDbContext _db;
        public AnnouncementReadService(ApplicationDbContext db) => _db = db;

        public async Task MarkAsReadAsync(int announcementId, string userId)
        {
            var exists = await _db.AnnouncementReads
                .AnyAsync(x => x.AnnouncementId == announcementId && x.UserId == userId);
            if (!exists)
            {
                _db.AnnouncementReads.Add(new Models.AnnouncementRead
                {
                    AnnouncementId = announcementId,
                    UserId = userId
                });
                await _db.SaveChangesAsync();
            }
        }

        public async Task<int> CountUnreadAsync(string userId)
        {
            // Conta anúncios publicados e não lidos pelo user.
            var published = _db.Announcements.Where(a => a.SendInApp && a.ScheduledAtUtc == null);
            var unread = from a in published
                         where !_db.AnnouncementReads.Any(r => r.AnnouncementId == a.Id && r.UserId == userId)
                         select a.Id;
            return await unread.CountAsync();
        }
    }
}

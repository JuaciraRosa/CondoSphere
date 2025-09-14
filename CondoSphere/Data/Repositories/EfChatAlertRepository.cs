using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Data.Repositories
{
    
    public class EfChatAlertRepository : IChatAlertRepository
    {
        private readonly ApplicationDbContext _db;
        public EfChatAlertRepository(ApplicationDbContext db) => _db = db;

        public async Task AddAsync(StaffChatAlert alert)
        {
            _db.StaffChatAlerts.Add(alert);
            await _db.SaveChangesAsync();
        }

        public Task<int> CountUnseenAsync(DateTime sinceUtc, int? condominiumId = null)
        {
            var q = _db.StaffChatAlerts.AsQueryable()
                .Where(a => a.SeenAtUtc == null && a.CreatedAtUtc >= sinceUtc);
            if (condominiumId.HasValue) q = q.Where(a => a.CondominiumId == condominiumId);
            return q.CountAsync();
        }

        public async Task MarkAllSeenAsync(int? condominiumId = null)
        {
            var q = _db.StaffChatAlerts.Where(a => a.SeenAtUtc == null);
            if (condominiumId.HasValue) q = q.Where(a => a.CondominiumId == condominiumId);

            var now = DateTime.UtcNow;
            await q.ExecuteUpdateAsync(u => u.SetProperty(a => a.SeenAtUtc, now));
        }
    }

}

using CondoSphere.Data;
using CondoSphere.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Services
{
    public class StaffDirectory : IStaffDirectory
    {
        private readonly ApplicationDbContext _db;
        public StaffDirectory(ApplicationDbContext db) => _db = db;

        public async Task<IReadOnlyList<string>> GetAdminAndManagerEmailsAsync(int? condominiumId = null)
        {
           
            var q =
                from u in _db.Users
                join ur in _db.UserRoles on u.Id equals ur.UserId
                join r in _db.Roles on ur.RoleId equals r.Id
                where r.Name == "Administrator" || r.Name == "Manager"
                select u.Email;

            return await q.Where(e => e != null && e != "")
                          .Distinct()
                          .ToListAsync();
        }


    //    public Task<bool> IsManagerOfCondominiumAsync(string userId, int condominiumId)
    //=> _db.CondominiumManagers
    //      .AnyAsync(x => x.UserId == userId && x.CondominiumId == condominiumId);
    }
}

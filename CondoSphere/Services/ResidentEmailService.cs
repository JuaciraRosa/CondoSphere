using CondoSphere.Data;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Services
{
    public class ResidentEmailService : IResidentEmailService
    {
        private readonly ApplicationDbContext _db;
        public ResidentEmailService(ApplicationDbContext db) => _db = db;

        public async Task<List<string>> GetResidentEmailsByCondoAsync(int condominiumId)
        {
            // pega e-mails distintos dos donos das frações do condomínio
            return await _db.Units
                .AsNoTracking()
                .Where(u => u.CondominiumId == condominiumId &&
                            u.OwnerId != null &&
                            u.Owner != null &&
                            !string.IsNullOrEmpty(u.Owner.Email))
                .Select(u => u.Owner!.Email!)
                .Distinct()
                .ToListAsync();
        }
    }

}

using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Data.Repositories
{
    public class CondominiumRepository : GenericRepository<Condominium>, ICondominiumRepository
    {
        public CondominiumRepository(ApplicationDbContext context) : base(context) { }
        public async Task<Condominium?> GetDetailsAsync(int id)
        {
            return await _context.Condominiums
                .Include(c => c.Company)                
                .Include(c => c.Units)
                .Include(c => c.Expenses)
                .Include(c => c.MaintenanceRequests)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<List<Condominium>> GetAllWithCompanyAsync()
        {
            return await _context.Condominiums
                .Include(c => c.Company)              
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();
        }


        public async Task<List<string>> GetOwnerEmailsAsync(int condominiumId)
        {
            return await _context.Units
                .Where(u => u.CondominiumId == condominiumId && u.OwnerId != null && u.Owner!.Email != null)
                .Select(u => u.Owner!.Email!)
                .Distinct()
                .ToListAsync();
        }



    }

}

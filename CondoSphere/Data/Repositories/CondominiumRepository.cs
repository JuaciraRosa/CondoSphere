using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Data.Repositories
{
    public class CondominiumRepository : GenericRepository<Condominium>, ICondominiumRepository
    {
        public CondominiumRepository(ApplicationDbContext context) : base(context) { }

        public async Task<Condominium> GetDetailsAsync(int id)
        {
            return await _context.Condominiums
                .Include(c => c.Units)
                .Include(c => c.Expenses)
                .Include(c => c.MaintenanceRequests)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public IQueryable<Company> Query()
        {
            return _context.Companies.AsQueryable();
        }
    }

}

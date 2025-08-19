using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace CondoSphere.Data.Repositories
{
    public class CompanyRepository : GenericRepository<Company>, ICompanyRepository
    {
        public CompanyRepository(ApplicationDbContext context) : base(context) { }

        public async Task<Company> GetWithCondominiumsAsync(int id)
        {
            return await _context.Companies
                .Include(c => c.Condominiums)
                    .ThenInclude(co => co.Units)   // ✅ só carrega Units
                .Include(c => c.Condominiums)
                    .ThenInclude(co => co.MaintenanceRequests) // ✅ MaintenanceRequests está em Condominium
                .FirstOrDefaultAsync(c => c.Id == id);
        }



        public IQueryable<Company> Query()
        {
            return _context.Companies.AsQueryable();
        }


    }

}

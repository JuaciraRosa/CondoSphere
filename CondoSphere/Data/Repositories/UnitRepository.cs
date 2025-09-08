using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Data.Repositories
{
    public class UnitRepository : GenericRepository<Unit>, IUnitRepository
    {
        public UnitRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Unit>> GetByCondominiumIdAsync(int condominiumId)
            => await _context.Units.Where(u => u.CondominiumId == condominiumId).ToListAsync();



        public async Task<IEnumerable<Unit>> GetAllDetailedAsync()
       => await _context.Units
           .AsNoTracking()
           .Include(u => u.Condominium)
           .Include(u => u.Owner)
           .OrderBy(u => u.Number)
           .ToListAsync();

        public async Task<Unit?> GetByIdDetailedAsync(int id)
            => await _context.Units
                .Include(u => u.Condominium)
                .Include(u => u.Owner)
                .FirstOrDefaultAsync(u => u.Id == id);


        public async Task<List<string>> GetNumbersByCondominiumIdAsync(int condominiumId)
     => await _context.Units
         .AsNoTracking()
         .Where(u => u.CondominiumId == condominiumId)
         .OrderBy(u => u.Number)
         .Select(u => u.Number)
         .ToListAsync();
    }

}

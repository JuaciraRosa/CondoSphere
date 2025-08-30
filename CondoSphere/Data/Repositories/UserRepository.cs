using CondoSphere.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Data.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context) { }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }


        public async Task<User?> GetByIdAsync(string id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public IQueryable<User> Query()
        {
            return _context.Users.AsQueryable();
        }


        public Task<User> GetByIdStringAsync(string id) =>
         _context.Users.FindAsync(id).AsTask();

        public async Task DeleteByIdStringAsync(string id)
        {
            var entity = await _context.Users.FindAsync(id);
            if (entity != null)
            {
                _context.Users.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

    }

}

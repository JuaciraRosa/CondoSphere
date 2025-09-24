using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Data.Repositories
{
    public class UnitOwnershipRepository : IUnitOwnershipRepository
    {
        private readonly ApplicationDbContext _ctx;
        public UnitOwnershipRepository(ApplicationDbContext ctx) => _ctx = ctx;

        public Task<List<UnitOwnership>> GetByUnitAsync(int unitId)
            => _ctx.UnitOwnerships
                   .Include(h => h.Owner)
                   .Where(h => h.UnitId == unitId)
                   .OrderByDescending(h => h.StartAt)
                   .ToListAsync();

        public async Task CloseOpenAsync(int unitId)
        {
            var open = await _ctx.UnitOwnerships
                .Where(h => h.UnitId == unitId && h.EndAt == null)
                .ToListAsync();
            foreach (var h in open) h.EndAt = DateTimeOffset.UtcNow;
        }

        public Task AddStartAsync(int unitId, string? ownerId)
        {
            _ctx.UnitOwnerships.Add(new UnitOwnership
            {
                UnitId = unitId,
                OwnerId = ownerId,
                StartAt = DateTimeOffset.UtcNow
            });
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync() => _ctx.SaveChangesAsync();

        public Task<bool> UserHasAnyUnitInCondoAsync(string userId, int condominiumId)
        {
            throw new NotImplementedException();
        }
    }
}

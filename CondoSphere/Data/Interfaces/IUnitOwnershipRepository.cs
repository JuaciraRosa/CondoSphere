using CondoSphere.Models;

namespace CondoSphere.Data.Interfaces
{
    public interface IUnitOwnershipRepository
    {
        Task<List<UnitOwnership>> GetByUnitAsync(int unitId);

        Task<bool> UserHasAnyUnitInCondoAsync(string userId, int condominiumId);

        Task CloseOpenAsync(int unitId);                   // fecha registros sem EndAt
        Task AddStartAsync(int unitId, string? ownerId);   // abre novo período
        Task SaveChangesAsync();
    }
}

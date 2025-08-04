using CondoSphere.Models;

namespace CondoSphere.Data.Interfaces
{
    public interface IUnitRepository : IGenericRepository<Unit>
    {
        Task<IEnumerable<Unit>> GetByCondominiumIdAsync(int condominiumId);
    }

}

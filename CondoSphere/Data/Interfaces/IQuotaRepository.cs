using CondoSphere.Models;

namespace CondoSphere.Data.Interfaces
{
    public interface IQuotaRepository : IGenericRepository<Quota>
    {
        Task<IEnumerable<Quota>> GetByUnitIdAsync(int unitId);
    }

}

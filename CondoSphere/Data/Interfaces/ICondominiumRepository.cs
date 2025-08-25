using CondoSphere.Models;

namespace CondoSphere.Data.Interfaces
{
    public interface ICondominiumRepository : IGenericRepository<Condominium>
    {
        Task<Condominium> GetDetailsAsync(int id);
       
    }

}

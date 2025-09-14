using CondoSphere.Models;

namespace CondoSphere.Data.Interfaces
{
    public interface ICondominiumRepository : IGenericRepository<Condominium>
    {
        Task<Condominium?> GetDetailsAsync(int id);

        Task<List<Condominium>> GetAllWithCompanyAsync();


        Task<List<string>> GetOwnerEmailsAsync(int condominiumId);


    }

}

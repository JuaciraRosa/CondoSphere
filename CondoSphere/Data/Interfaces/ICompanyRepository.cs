using CondoSphere.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace CondoSphere.Data.Interfaces
{
    public interface ICompanyRepository : IGenericRepository<Company>
    {
        Task<Company> GetWithCondominiumsAsync(int id);
    }

}

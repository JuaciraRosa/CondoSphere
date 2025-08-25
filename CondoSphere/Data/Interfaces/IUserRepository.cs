namespace CondoSphere.Data.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User> GetByEmailAsync(string email);
  
        IQueryable<User> Query();

        // Sobrecarga para deletar por string (porque User.Id é string)
     

        Task<User> GetByIdStringAsync(string id);
        Task DeleteByIdStringAsync(string id);



    }

}

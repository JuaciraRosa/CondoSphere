namespace CondoSphere.Data.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User> GetByEmailAsync(string email);
        IQueryable<User> Query();

        // helpers por string
        Task<User?> GetByIdAsync(string id);          // overload para Identity
        Task<User> GetByIdStringAsync(string id);     // opcional
        Task DeleteByIdStringAsync(string id);        // opcional
    }

}

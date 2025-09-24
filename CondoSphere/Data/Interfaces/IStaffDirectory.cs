namespace CondoSphere.Data.Interfaces
{
    public interface IStaffDirectory
    {
       
        Task<IReadOnlyList<string>> GetAdminAndManagerEmailsAsync(int? condominiumId = null);

        //Task<bool> IsManagerOfCondominiumAsync(string userId, int condominiumId);
    }
}

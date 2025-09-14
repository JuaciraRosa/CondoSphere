namespace CondoSphere.Data.Interfaces
{
    public interface IStaffDirectory
    {
       
        Task<IReadOnlyList<string>> GetAdminAndManagerEmailsAsync(int? condominiumId = null);
    }
}

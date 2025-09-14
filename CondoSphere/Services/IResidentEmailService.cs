namespace CondoSphere.Services
{
    public interface IResidentEmailService
    {
        Task<List<string>> GetResidentEmailsByCondoAsync(int condominiumId);
    }
}

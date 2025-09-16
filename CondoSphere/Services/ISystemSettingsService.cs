using CondoSphere.Models;

namespace CondoSphere.Services
{
    public interface ISystemSettingsService
    {
        Task<SystemSettings> GetCurrentAsync();
        Task UpdateAsync(SystemSettings input);
        string RenderTemplate(string? html, IDictionary<string, string> data);
    }
}

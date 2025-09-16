using CondoSphere.Data;

namespace CondoSphere.Services
{
    public interface IJwtTokenService
    {
        Task<string> IssueAsync(User user);
    }

}

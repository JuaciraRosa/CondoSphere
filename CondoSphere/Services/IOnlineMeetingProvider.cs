namespace CondoSphere.Services
{
    public interface IOnlineMeetingProvider
    {
        Task<OnlineMeetingResult> CreateAsync(CondoSphere.Models.Meeting meeting, CancellationToken ct = default);
    }
}

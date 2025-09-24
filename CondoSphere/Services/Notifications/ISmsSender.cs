namespace CondoSphere.Services.Notifications
{
    public interface ISmsSender
    {
        Task SendAsync(string toE164, string body, CancellationToken ct = default);
    }
}

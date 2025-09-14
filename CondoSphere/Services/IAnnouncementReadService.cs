namespace CondoSphere.Services
{
    public interface IAnnouncementReadService
    {
        Task MarkAsReadAsync(int announcementId, string userId);
        Task<int> CountUnreadAsync(string userId);
    }
}

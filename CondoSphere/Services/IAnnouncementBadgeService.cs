namespace CondoSphere.Services
{
    public interface IAnnouncementBadgeService
    {
        Task<int> CountRecentAsync(string userId, int days = 3);
    }
}

namespace CondoSphere.Data.Interfaces
{
    public interface IChatAlertService
    {
        Task CreateAlertAndNotifyAsync(
            string residentId,
            string residentEmail,
            string messagePreview,
            string chatDeeplink,
            int? condominiumId = null);

        Task<int> CountUnseenAsync(int days, int? condominiumId = null);
        Task MarkAllSeenAsync(int? condominiumId = null);
    }
}

using CondoSphere.Data.Interfaces;
using CondoSphere.Messaging;
using CondoSphere.Models;

namespace CondoSphere.Services
{
    
    public class ChatAlertService : IChatAlertService
    {
        private readonly IChatAlertRepository _repo;
        private readonly IStaffDirectory _staff;
        private readonly DomainNotificationService _notify;

        public ChatAlertService(IChatAlertRepository repo,
                                IStaffDirectory staff,
                                DomainNotificationService notify)
        {
            _repo = repo; _staff = staff; _notify = notify;
        }

        public async Task CreateAlertAndNotifyAsync(
            string residentId,
            string residentEmail,
            string messagePreview,
            string chatDeeplink,
            int? condominiumId = null)
        {
            var preview = (messagePreview ?? "").Trim();
            if (preview.Length > 120) preview = preview[..120] + "…";

            await _repo.AddAsync(new StaffChatAlert
            {
                ResidentId = residentId,
                MessagePreview = preview,
                CondominiumId = condominiumId
            });

            var recipients = await _staff.GetAdminAndManagerEmailsAsync(condominiumId);
            if (recipients.Count == 0) return;

            await _notify.StaffNewChatMessageAsync(recipients, residentEmail, preview, chatDeeplink);
        }

        public Task<int> CountUnseenAsync(int days, int? condominiumId = null)
            => _repo.CountUnseenAsync(DateTime.UtcNow.AddDays(-days), condominiumId);

        public Task MarkAllSeenAsync(int? condominiumId = null)
            => _repo.MarkAllSeenAsync(condominiumId);
    }

}

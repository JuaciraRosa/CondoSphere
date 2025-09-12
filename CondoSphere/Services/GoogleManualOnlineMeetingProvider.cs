using CondoSphere.Models;

namespace CondoSphere.Services
{
    public class GoogleManualOnlineMeetingProvider : IOnlineMeetingProvider
    {
      

        public async  Task<OnlineMeetingResult> CreateAsync(Meeting meeting, CancellationToken ct = default)
        {

            if (string.IsNullOrWhiteSpace(meeting.OnlineJoinUrl))
                throw new InvalidOperationException("Informe o Join URL do Google Meet.");

            return await Task.FromResult(new OnlineMeetingResult(
                Provider: "Google",
                ExternalId: Guid.NewGuid().ToString("N"),
                JoinUrl: meeting.OnlineJoinUrl!,
                StartUrl: null));
        }
    }
}

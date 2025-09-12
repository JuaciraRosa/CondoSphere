using CondoSphere.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CondoSphere.Services
{
    public class ZoomOnlineMeetingProvider : IOnlineMeetingProvider
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _cfg;

        // === Cache do token (compartilhado no processo) ===
        private static string? _cachedToken;
        private static DateTime _tokenExpiresAtUtc;
        private static readonly SemaphoreSlim _tokenLock = new(1, 1);

        public ZoomOnlineMeetingProvider(IHttpClientFactory httpFactory, IConfiguration cfg)
        {
            _httpFactory = httpFactory;
            _cfg = cfg;
        }

        private async Task<string> GetAccessTokenAsync(CancellationToken ct)
        {
            // Reusa se ainda estiver válido
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiresAtUtc)
                return _cachedToken!;

            await _tokenLock.WaitAsync(ct);
            try
            {
                // Double-check depois de pegar o lock
                if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiresAtUtc)
                    return _cachedToken!;

                var accountId = _cfg["OnlineMeetings:Zoom:AccountId"];
                var clientId = _cfg["OnlineMeetings:Zoom:ClientId"];
                var clientSecret = _cfg["OnlineMeetings:Zoom:ClientSecret"];
                if (string.IsNullOrWhiteSpace(accountId) ||
                    string.IsNullOrWhiteSpace(clientId) ||
                    string.IsNullOrWhiteSpace(clientSecret))
                {
                    throw new InvalidOperationException("Zoom Server-to-Server OAuth não configurado.");
                }

                var http = _httpFactory.CreateClient();
                var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
                var req = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"https://zoom.us/oauth/token?grant_type=account_credentials&account_id={accountId}"
                );
                req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

                using var res = await http.SendAsync(req, ct);
                res.EnsureSuccessStatusCode();

                await using var s = await res.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(s, cancellationToken: ct);

                _cachedToken = doc.RootElement.GetProperty("access_token").GetString()!;
                var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var e) ? e.GetInt32() : 3600;

                // Margem de segurança de 60s
                var seconds = Math.Max(60, expiresIn - 60);
                _tokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(seconds);

                return _cachedToken!;
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        public async Task<OnlineMeetingResult> CreateAsync(Meeting meeting, CancellationToken ct = default)
        {
            // título/horário corretos
            var topic = string.IsNullOrWhiteSpace(meeting?.Agenda)
                ? "CondoSphere Meeting"
                : meeting!.Agenda.Trim();

            var startTimeUtc = meeting.ScheduledDate.Kind == DateTimeKind.Utc
                ? meeting.ScheduledDate
                : DateTime.SpecifyKind(meeting.ScheduledDate, DateTimeKind.Local).ToUniversalTime();

            var token = await GetAccessTokenAsync(ct);

            var http = _httpFactory.CreateClient();
            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.zoom.us/v2/users/me/meetings");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var payload = new
            {
                topic = topic,
                type = 2, // scheduled
                start_time = startTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                duration = 60, // minutos
                settings = new
                {
                    join_before_host = false,
                    waiting_room = true
                }
            };
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var res = await http.SendAsync(req, ct);
            res.EnsureSuccessStatusCode();

            await using var s = await res.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(s, cancellationToken: ct);

            // id vem numérico; convertemos para string
            var id = doc.RootElement.GetProperty("id").GetInt64().ToString();
            var joinUrl = doc.RootElement.GetProperty("join_url").GetString()!;
            var startUrl = doc.RootElement.TryGetProperty("start_url", out var su) ? su.GetString() : null;

            return new OnlineMeetingResult("Zoom", id, joinUrl, startUrl);
        }
    }
}

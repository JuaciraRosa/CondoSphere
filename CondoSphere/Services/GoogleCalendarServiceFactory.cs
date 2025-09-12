using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;

namespace CondoSphere.Services
{
    public class GoogleCalendarServiceFactory
    {
        private readonly IConfiguration _cfg;
        public GoogleCalendarServiceFactory(IConfiguration cfg) => _cfg = cfg;

        public async Task<CalendarService> CreateAsync(CancellationToken ct = default)
        {
            var cid = _cfg["OnlineMeetings:Google:ClientId"]!;
            var csec = _cfg["OnlineMeetings:Google:ClientSecret"]!;
            var rtok = _cfg["OnlineMeetings:Google:RefreshToken"]!;

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets { ClientId = cid, ClientSecret = csec },
                Scopes = new[] { CalendarService.Scope.Calendar }
            });

            var cred = new UserCredential(flow, "admin", new TokenResponse { RefreshToken = rtok });
            await cred.RefreshTokenAsync(ct); // garante access_token válido

            return new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = cred,
                ApplicationName = "CondoSphere"
            });
        }
    }
}

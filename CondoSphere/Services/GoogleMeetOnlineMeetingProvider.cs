using CondoSphere.Models;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;

namespace CondoSphere.Services
{
    public class GoogleMeetOnlineMeetingProvider : IOnlineMeetingProvider
    {
        private readonly GoogleCalendarServiceFactory _factory;
        private readonly IConfiguration _cfg;

        public GoogleMeetOnlineMeetingProvider(GoogleCalendarServiceFactory factory, IConfiguration cfg)
        {
            _factory = factory; _cfg = cfg;
        }

        public async Task<OnlineMeetingResult> CreateAsync(Meeting meeting, CancellationToken ct = default)
        {
            var svc = await _factory.CreateAsync(ct);
            var tz = _cfg["OnlineMeetings:Google:TimeZone"] ?? "UTC";
            var calendarId = _cfg["OnlineMeetings:Google:CalendarId"] ?? "primary";

            var ev = new Event
            {
                Summary = meeting.Agenda,
                Description = $"Condominium #{meeting.CondominiumId} meeting",
                Start = new EventDateTime { DateTime = meeting.ScheduledDate, TimeZone = tz },
                End = new EventDateTime { DateTime = meeting.ScheduledDate.AddHours(1), TimeZone = tz },
                ConferenceData = new ConferenceData
                {
                    CreateRequest = new CreateConferenceRequest
                    {
                        RequestId = Guid.NewGuid().ToString("N"),
                        ConferenceSolutionKey = new ConferenceSolutionKey { Type = "hangoutsMeet" }
                    }
                }
            };

            var req = svc.Events.Insert(ev, calendarId);
            req.ConferenceDataVersion = 1;

            Event created;
            try
            {
                created = await req.ExecuteAsync(ct);
            }
            catch (GoogleApiException ex) when ((int)ex.HttpStatusCode == 403 || (int)ex.HttpStatusCode == 400)
            {
                // Conta pessoal? Admin do Workspace não permitiu criação? Cai no manual.
                throw new InvalidOperationException(
                    "Não foi possível gerar o link do Google Meet via API. " +
                    "Isso normalmente requer Google Workspace com Meet habilitado para o domínio. " +
                    "Use o provedor 'Google (manual)' e cole o link do Meet.", ex);
            }

            var joinUrl = created.ConferenceData?.EntryPoints?
                              .FirstOrDefault(ep => ep.EntryPointType == "video")?.Uri
                          ?? created.HangoutLink;

            return new OnlineMeetingResult(
                Provider: "Google",
                ExternalId: created.Id!,
                JoinUrl: joinUrl!,
                StartUrl: null); // Meet não tem 'startUrl' separado
        }
    }
}

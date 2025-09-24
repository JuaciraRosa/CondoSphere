using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Clients;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace CondoSphere.Services.Notifications
{
    public class TwilioSmsSender : ISmsSender
    {
        private readonly TwilioSmsOptions _opt;
        private readonly ILogger<TwilioSmsSender> _log;
        private readonly ITwilioRestClient _client;

        public TwilioSmsSender(IOptions<TwilioSmsOptions> opt, ILogger<TwilioSmsSender> log)
        {
            _opt = opt.Value;
            _log = log;

            if (string.IsNullOrWhiteSpace(_opt.AccountSid) || string.IsNullOrWhiteSpace(_opt.AuthToken))
                throw new InvalidOperationException("Twilio credentials are missing.");

            var handler = new System.Net.Http.HttpClientHandler
            {
               
            };

            _client = new TwilioRestClient(
      _opt.AccountSid, _opt.AuthToken,
      httpClient: new Twilio.Http.SystemNetHttpClient(new System.Net.Http.HttpClient(handler)),
      region: string.IsNullOrWhiteSpace(_opt.Region) ? null : _opt.Region);

        }

        public async Task SendAsync(string toE164, string body, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(toE164) || !toE164.StartsWith("+"))
                throw new ArgumentException("Phone must be in E.164 format, e.g. +3519XXXXXXXX.", nameof(toE164));
            if (string.IsNullOrWhiteSpace(body))
                throw new ArgumentException("Body is required.", nameof(body));

            var msgOpts = new CreateMessageOptions(new PhoneNumber(toE164))
            {
                Body = body
            };

            if (!string.IsNullOrWhiteSpace(_opt.MessagingServiceSid))
                msgOpts.MessagingServiceSid = _opt.MessagingServiceSid;
            else if (!string.IsNullOrWhiteSpace(_opt.FromNumber))
                msgOpts.From = new PhoneNumber(_opt.FromNumber);
            else
                throw new InvalidOperationException("Configure FromNumber or MessagingServiceSid.");

            try
            {
                // opcional: honrar cancelamento antes da chamada
                ct.ThrowIfCancellationRequested();

                var message = await MessageResource.CreateAsync(msgOpts, _client);

                _log.LogInformation("Twilio SMS queued. Sid={Sid}, To={To}, Status={Status}", message.Sid, toE164, message.Status);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Twilio SMS failed. To={To}", toE164);
                throw; // propaga para a app decidir retry/fallback
            }
        }
    }
}

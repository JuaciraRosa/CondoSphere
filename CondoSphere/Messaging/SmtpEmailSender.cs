using System.Net.Mail;
using System.Net;

namespace CondoSphere.Messaging
{

    // Simple SMTP sender using settings from appsettings.json:
    // "Smtp": { "Host": "...", "Port": 587, "User": "...", "Pass": "...", "From": "no-reply@..." , "EnableSsl": true }
    // IEmailSender já existe no teu projeto
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _cfg;
        private readonly ILogger<SmtpEmailSender> _log;

        public SmtpEmailSender(IConfiguration cfg, ILogger<SmtpEmailSender> log)
        {
            _cfg = cfg;
            _log = log;
        }

        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            var host = _cfg["Smtp:Host"];
            var port = int.TryParse(_cfg["Smtp:Port"], out var p) ? p : 26;
            var user = _cfg["Smtp:User"];
            var pass = _cfg["Smtp:Pass"];
            var from = _cfg["Smtp:From"];
            var enableSsl = bool.TryParse(_cfg["Smtp:EnableSsl"], out var ssl) && ssl;

            using var msg = new MailMessage();
            msg.From = new MailAddress(from);
            msg.To.Add(to);
            msg.Subject = subject;
            msg.Body = htmlBody;
            msg.IsBodyHtml = true;

            using var client = new SmtpClient(host, port)
            {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(user, pass),
                EnableSsl = enableSsl // Somee: false
            };

            try
            {
                await client.SendMailAsync(msg);
                _log.LogInformation("Email enviado para {To} via {Host}:{Port}", to, host, port);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Falha ao enviar email para {To} via {Host}:{Port}", to, host, port);
                throw; // deixe propagar para veres no log se necessário
            }
        }
    }
}

using System.Globalization;

namespace CondoSphere.Messaging
{
    public class DomainNotificationService
    {
        private readonly IEmailSender _email;
        public DomainNotificationService(IEmailSender email) => _email = email;

        public Task PaymentReceivedAsync(string to, int paymentId, decimal amount)
        {
            var subject = $"Pagamento confirmado #{paymentId:D6}";
            var body = $@"<h3>Pagamento confirmado</h3>
                          <p>O pagamento <strong>#{paymentId:D6}</strong> foi confirmado no valor de
                          <strong>{amount.ToString("C", new CultureInfo("pt-PT"))}</strong>.</p>";
            return _email.SendAsync(to, subject, body);
        }

        public Task MeetingMinutesPublishedAsync(string to, int meetingId, string publicUrl)
        {
            var subject = $"Ata disponível — Reunião #{meetingId}";
            var body = $@"<h3>Documento publicado</h3>
                          <p>A ata da reunião <strong>#{meetingId}</strong> está disponível.</p>
                          <p><a href=""{publicUrl}"">Abrir documento</a></p>";
            return _email.SendAsync(to, subject, body);
        }

        public Task OccurrenceStatusChangedAsync(string to, string occurrenceTitle, string newStatus)
        {
            var subject = $"Ocorrência atualizada — {newStatus}";
            var body = $@"<h3>Estado atualizado</h3>
                          <p>A sua ocorrência <strong>{occurrenceTitle}</strong> foi atualizada para
                          <strong>{newStatus}</strong>.</p>";
            return _email.SendAsync(to, subject, body);
        }
    }
}

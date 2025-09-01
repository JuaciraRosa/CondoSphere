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

        // NOVO: e-mail de obrigado por votar
        public Task VotingThankYouAsync(string to, int meetingId, string unitNumber, string choice)
        {
            var subject = $"Obrigado por votar — Reunião #{meetingId}";
            var body =
    $@"<h3>Obrigado pela sua participação</h3>
<p>O seu voto foi registado com sucesso.</p>
<ul>
  <li><strong>Reunião:</strong> #{meetingId}</li>
  <li><strong>Unidade:</strong> {unitNumber}</li>
  <li><strong>Voto:</strong> {choice}</li>
</ul>
<p>CondoSphere</p>";

            return _email.SendAsync(to, subject, body);
        }



        public Task MaintenanceRequestReceivedAsync(string to, string title, int requestId, string condoName)
        {
            if (string.IsNullOrWhiteSpace(to)) return Task.CompletedTask;

            var subject = $"Pedido de manutenção recebido — #{requestId:D6}";
            var body = $@"<h3>Obrigado!</h3>
                  <p>Recebemos o seu pedido de manutenção <strong>#{requestId:D6}</strong>.</p>
                  <p><strong>Condomínio:</strong> {condoName}<br/>
                     <strong>Título:</strong> {title}</p>
                  <p>Em breve a administração irá acompanhar o caso.</p>";
            return _email.SendAsync(to, subject, body);
        }

    }
}

using CondoSphere.Models;
using CondoSphere.Services.Notifications;
using System.Globalization;
using System.Net;
using System.Net.Mail;

namespace CondoSphere.Messaging
{
    public class DomainNotificationService
    {
        private readonly IEmailSender _email;
        private readonly ISmsSender _sms;
        public DomainNotificationService(IEmailSender email, ISmsSender sms)
        {
            _email = email;
            _sms = sms;
        }


        public Task NotifyResidentAsync(string phoneE164, string text, CancellationToken ct = default)
        => _sms.SendAsync(phoneE164, text, ct);

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


        public Task SendAsync(string to, string subject, string htmlBody)
        {
            
            return _email.SendAsync(to, subject, htmlBody);
        }


        /// <summary>
        /// Envia um comunicado por e-mail. Se 'recipients' for nulo, não faz nada.
        /// </summary>
        public Task SendAnnouncementEmailAsync(
           string subject,
           string htmlBody,
           int? condoId = null,
           string? attachmentUrl = null,
           IEnumerable<string>? recipients = null)
        {
            if (recipients is null) return Task.CompletedTask;

            var list = recipients
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (list.Count == 0) return Task.CompletedTask;

            // Envia UM e-mail com todos em BCC (o link do anexo é tratado no SmtpEmailSender)
            return _email.SendBulkBccAsync(list, subject, htmlBody, attachmentUrl);
        }


        /// <summary>
        /// Notificação in-app (placeholder). Integre aqui seu mecanismo real de notificação.
        /// </summary>
        public Task PushInAppAsync(
            string title,
            string message,
            int? condoId = null,
            string? deeplink = null)
        {
           
            return Task.CompletedTask;
        }


        public Task PollVoteReceiptAsync(string to, string pollTitle, string optionText)
        {
            var subject = $"Confirmação de voto — {pollTitle}";
            var html = $@"
        <p>Olá,</p>
        <p>Recebemos o seu voto na enquete <strong>{System.Net.WebUtility.HtmlEncode(pollTitle)}</strong>.</p>
        <p>Opção escolhida: <strong>{System.Net.WebUtility.HtmlEncode(optionText)}</strong></p>
        <p>Obrigado pela participação.</p>";
            return SendAsync(to, subject, html);
        }


        public async Task MeetingScheduledAsync(IEnumerable<string> recipients, Meeting meeting)
        {
            if (recipients == null) return;

            var when = meeting.ScheduledDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            var subject = $"[CondoSphere] Reunião marcada — {when}";

            var agenda = System.Net.WebUtility.HtmlEncode(meeting.Agenda ?? "");

            string linkHtml = "";
            if (meeting.IsOnline &&
                !string.IsNullOrWhiteSpace(meeting.OnlineJoinUrl) &&
                Uri.TryCreate(meeting.OnlineJoinUrl, UriKind.Absolute, out var uri))
            {
                var url = System.Net.WebUtility.HtmlEncode(uri.ToString());
                linkHtml =
                    $@"<p><strong>Link de participação:</strong>
                 <a href=""{url}"" target=""_blank"" rel=""noopener"">{url}</a>
               </p>";
            }

            var html =
        $@"<p>Foi agendada uma reunião para <strong>{when}</strong>.</p>
<p><strong>Pauta:</strong> {agenda}</p>
{linkHtml}";

            foreach (var to in recipients.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                await SendAsync(to, subject, html); // 3 argumentos
            }
        }


        public async Task MaintenanceRequestStatusChangedAsync(
    string to, MaintenanceRequest mr, string oldStatus)
        {
            var subject = $"[CondoSphere] Pedido de Manutenção #{mr.Id} atualizado para {mr.Status}";
            var title = System.Net.WebUtility.HtmlEncode(mr.Title ?? "");

            var html =
        $@"<p>O seu pedido de manutenção <strong>#{mr.Id} — {title}</strong> foi atualizado.</p>
<p><strong>Estado anterior:</strong> {oldStatus}<br/>
<strong>Novo estado:</strong> {mr.Status}</p>";

            await SendAsync(to, subject, html);   // 3 argumentos
        }

        public async Task MaintenanceRequestStatusChangedAsync(
            IEnumerable<string> recipients, MaintenanceRequest mr, string oldStatus)
        {
            if (recipients == null) return;

            foreach (var to in recipients.Where(e => !string.IsNullOrWhiteSpace(e))
                                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                await MaintenanceRequestStatusChangedAsync(to, mr, oldStatus);
            }
        }

        // Services/DomainNotificationService.cs (adicione)
        public async Task StaffNewChatMessageAsync(
            IEnumerable<string> recipients,
            string residentEmail,
            string messagePreview,
            string chatLink)
        {
            if (recipients == null) return;

            var subject = "[CondoSphere] Nova mensagem do residente no chat";
            var who = System.Net.WebUtility.HtmlEncode(residentEmail ?? "");
            var prev = System.Net.WebUtility.HtmlEncode(messagePreview ?? "");
            var link = System.Net.WebUtility.HtmlEncode(chatLink ?? "/Chat");

            var html = $@"
        <p>O residente <strong>{who}</strong> enviou uma nova mensagem no chat.</p>
        <p><em>{prev}</em></p>
        <p><a href=""{link}"" target=""_blank"" rel=""noopener"">Abrir o chat</a></p>";

            foreach (var to in recipients.Where(e => !string.IsNullOrWhiteSpace(e))
                                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                await SendAsync(to!, subject, html); // seu SendAsync(to, subject, html)
            }
        }


        // Overload: meeting scheduled WITH optional attachment link
        public async Task MeetingScheduledAsync(IEnumerable<string> recipients, Meeting meeting, string? attachmentUrl)
        {
            if (recipients == null) return;

            var when = meeting.ScheduledDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            var subject = $"[CondoSphere] Meeting scheduled — {when}";

            var agenda = WebUtility.HtmlEncode(meeting.Agenda ?? "");

            string joinHtml = "";
            if (meeting.IsOnline && !string.IsNullOrWhiteSpace(meeting.OnlineJoinUrl))
            {
                var url = WebUtility.HtmlEncode(meeting.OnlineJoinUrl);
                joinHtml = $@"<p><strong>Join link:</strong> <a href=""{url}"" target=""_blank"" rel=""noopener"">{url}</a></p>";
            }

            string attachHtml = string.IsNullOrWhiteSpace(attachmentUrl)
                ? ""
                : $@"<p><strong>Attachment:</strong> <a href=""{WebUtility.HtmlEncode(attachmentUrl)}"">Download</a></p>";

            var html = $@"
<p>A meeting has been scheduled for <strong>{when}</strong>.</p>
<p><strong>Agenda:</strong> {agenda}</p>
{joinHtml}
{attachHtml}";

            foreach (var to in recipients.Where(e => !string.IsNullOrWhiteSpace(e))
                                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                await _email.SendAsync(to, subject, html);
            }
        }

        // Meeting updated (optionally include that a new attachment was added)
        public async Task MeetingUpdatedAsync(IEnumerable<string> recipients, Meeting meeting, bool attachmentAdded, string? attachmentUrl = null)
        {
            if (recipients == null) return;

            var when = meeting.ScheduledDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            var subject = $"[CondoSphere] Meeting updated — {when}";

            var agenda = WebUtility.HtmlEncode(meeting.Agenda ?? "");

            string joinHtml = "";
            if (meeting.IsOnline && !string.IsNullOrWhiteSpace(meeting.OnlineJoinUrl))
            {
                var url = WebUtility.HtmlEncode(meeting.OnlineJoinUrl);
                joinHtml = $@"<p><strong>Join link:</strong> <a href=""{url}"">{url}</a></p>";
            }

            string attachHtml = attachmentAdded && !string.IsNullOrWhiteSpace(attachmentUrl)
                ? $@"<p><strong>New attachment added:</strong> <a href=""{WebUtility.HtmlEncode(attachmentUrl)}"">Download</a></p>"
                : "";

            var html = $@"
<p>A meeting has been <strong>updated</strong> for <strong>{when}</strong>.</p>
<p><strong>Agenda:</strong> {agenda}</p>
{joinHtml}
{attachHtml}";

            foreach (var to in recipients.Where(e => !string.IsNullOrWhiteSpace(e))
                                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                await _email.SendAsync(to, subject, html);
            }
        }

        // Meeting canceled (deleted)
        public async Task MeetingCanceledAsync(IEnumerable<string> recipients, Meeting meeting)
        {
            if (recipients == null) return;

            var when = meeting.ScheduledDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            var subject = $"[CondoSphere] Meeting canceled — {when}";

            var agenda = WebUtility.HtmlEncode(meeting.Agenda ?? "");
            var html = $@"
<p>The meeting scheduled for <strong>{when}</strong> has been <strong>canceled</strong>.</p>
<p><strong>Agenda:</strong> {agenda}</p>";

            foreach (var to in recipients.Where(e => !string.IsNullOrWhiteSpace(e))
                                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                await _email.SendAsync(to, subject, html);
            }
        }

        // == CRIAÇÃO ==
        public Task AnnouncementCreatedAsync(
       IEnumerable<string> recipients,
       Announcement a,
       string? attachmentUrl)
        {
            return AnnouncementCreatedAsync(recipients, a, attachmentUrl, bodyHtml: null);
        }

        public async Task AnnouncementCreatedAsync(
            IEnumerable<string> recipients,
            Announcement a,
            string? attachmentUrl,
            string? bodyHtml)
        {
            if (recipients is null) return;

            var list = recipients
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (list.Count == 0) return;

            var subject = $"[CondoSphere] Announcement — {a.Title}";
            var body = $@"
<p>New announcement was posted.</p>
<p><strong>Title:</strong> {System.Net.WebUtility.HtmlEncode(a.Title ?? "Untitled")}</p>
{(string.IsNullOrWhiteSpace(bodyHtml) ? "" : bodyHtml)}";

            try
            {
                await _email.SendBulkBccAsync(list, subject, body, attachmentUrl);
            }
            catch (SmtpException)
            {
                foreach (var to in list)
                    await _email.SendAsync(to, subject,
                        body + (string.IsNullOrWhiteSpace(attachmentUrl) ? "" : $@"<p><a href=""{attachmentUrl}"">Attachment</a></p>"));
            }
        }





        public Task AnnouncementUpdatedAsync(
            IEnumerable<string> recipients,
            Announcement a,
            bool attachmentChanged,
            string? attachmentUrl,
            string? bodyHtml = null)
        {
            if (recipients is null) return Task.CompletedTask;

            var list = recipients
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (list.Count == 0) return Task.CompletedTask;

            var subject = $"[CondoSphere] Announcement updated — {a.Title}";
            var attachBlock = attachmentChanged
                ? (string.IsNullOrWhiteSpace(attachmentUrl)
                    ? "<p><em>Attachment removed.</em></p>"
                    : "<p><strong>Attachment updated:</strong> see link below.</p>")
                : "";

            var body = $@"
<p>An announcement has been updated.</p>
<p><strong>Title:</strong> {System.Net.WebUtility.HtmlEncode(a.Title ?? "Untitled")}</p>
{(string.IsNullOrWhiteSpace(bodyHtml) ? "" : bodyHtml)}
{attachBlock}
";

            // O link de anexo (se houver) será adicionado pelo SmtpEmailSender
            return _email.SendBulkBccAsync(list, subject, body, attachmentUrl);
        }

        public Task AnnouncementDeletedAsync(IEnumerable<string> recipients, Announcement a)
        {
            if (recipients is null) return Task.CompletedTask;

            var list = recipients
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (list.Count == 0) return Task.CompletedTask;

            var subject = $"[CondoSphere] Announcement canceled — {a.Title}";
            var body = $@"
<p>The following announcement was canceled/removed.</p>
<p><strong>Title:</strong> {System.Net.WebUtility.HtmlEncode(a.Title ?? "Untitled")}</p>";

            return _email.SendBulkBccAsync(list, subject, body);
        }



        public async Task ForumNewTopicAsync(IEnumerable<string> recipients, ForumTopic topic, string authorEmail, string topicUrl)
        {
            if (recipients == null) return;
            var subj = $"[Forum] New topic: {topic.Title}";
            var html = $@"
<p>New topic created by <strong>{System.Net.WebUtility.HtmlEncode(authorEmail)}</strong>.</p>
<p><a href=""{System.Net.WebUtility.HtmlEncode(topicUrl)}"">Open topic</a></p>";
            foreach (var to in recipients.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
                await SendAsync(to, subj, html);
        }

        public async Task ForumNewReplyAsync(IEnumerable<string> recipients, ForumTopic topic, string authorEmail, string topicUrl, string preview)
        {
            if (recipients == null) return;
            var subj = $"[Forum] New reply in: {topic.Title}";
            var html = $@"
<p>New reply by <strong>{System.Net.WebUtility.HtmlEncode(authorEmail)}</strong>.</p>
<p><em>{System.Net.WebUtility.HtmlEncode(preview)}</em></p>
<p><a href=""{System.Net.WebUtility.HtmlEncode(topicUrl)}"">Open topic</a></p>";
            foreach (var to in recipients.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
                await SendAsync(to, subj, html);
        }

        public async Task ForumNewPostAsync(IEnumerable<string> recipients, string topicTitle, string messagePreview, string topicUrl)
        {
            if (recipients == null) return;
            var subject = $"[Forum] New reply — {topicTitle}";
            var html = $@"
        <p>There is a new reply in <strong>{System.Net.WebUtility.HtmlEncode(topicTitle)}</strong>.</p>
        <blockquote>{System.Net.WebUtility.HtmlEncode(messagePreview)}</blockquote>
        <p><a href=""{System.Net.WebUtility.HtmlEncode(topicUrl)}"">Open topic</a></p>";

            foreach (var to in recipients.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct(StringComparer.OrdinalIgnoreCase))
                await _email.SendAsync(to, subject, html);
        }

        public async Task ForumMentionAsync(IEnumerable<string> recipients, string topicTitle, string messagePreview, string topicUrl)
        {
            if (recipients == null) return;
            var subject = $"[Forum] You were mentioned — {topicTitle}";
            var html = $@"
        <p>You were mentioned in <strong>{System.Net.WebUtility.HtmlEncode(topicTitle)}</strong>.</p>
        <blockquote>{System.Net.WebUtility.HtmlEncode(messagePreview)}</blockquote>
        <p><a href=""{System.Net.WebUtility.HtmlEncode(topicUrl)}"">Open topic</a></p>";

            foreach (var to in recipients.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct(StringComparer.OrdinalIgnoreCase))
                await _email.SendAsync(to, subject, html);
        }








    }







}





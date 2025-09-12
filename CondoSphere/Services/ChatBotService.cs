using CondoSphere.Messaging;
using CondoSphere.Models;

namespace CondoSphere.Services
{
    public class ChatBotService : IChatBotService
    {
        private readonly DomainNotificationService _notify;
        public ChatBotService(DomainNotificationService notify) => _notify = notify;

        public async Task<string?> BuildReplyAsync(ChatThread thread, ChatMessage lastUserMessage)
        {
            var txt = (lastUserMessage.Text ?? "").ToLowerInvariant();

            if (txt.Contains("elevador") || txt.Contains("avaria") || txt.Contains("manuten"))
            {
                // AGORA aguardando o envio do e-mail
                await _notify.MaintenanceRequestReceivedAsync(
                    to: "support@condosphere-web-app.somee.com",
                    title: "Avaria reportada via chat",
                    requestId: thread.Id,
                    condoName: thread.CondominiumId?.ToString() ?? "—");

                return "Obrigado. Registámos a sua avaria e a administração foi notificada. Entraremos em contacto.";
            }

            if (txt.Contains("barulho") || txt.Contains("ruido") || txt.Contains("ruído"))
                return "Obrigado pelo aviso. Vamos avaliar a situação conforme o regulamento do condomínio.";

            if (txt.Contains("quota") || txt.Contains("pagamento") || txt.Contains("mensalidade"))
                return "Para pagar a quota: menu Quotas → Pay. Se preferir, peça que um gestor envie o link.";

            if (txt.Contains("documento") || txt.Contains("ata") || txt.Contains("regulamento"))
                return "A administração pode enviar os documentos sob pedido. Encaminhei sua solicitação.";

            return null;
        }

    }
}

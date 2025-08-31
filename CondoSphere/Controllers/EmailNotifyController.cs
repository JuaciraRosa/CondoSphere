using CondoSphere.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CondoSphere.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class EmailNotifyController : Controller
    {
        private readonly IEmailSender _email;
        public EmailNotifyController(IEmailSender email) => _email = email;

        // GET /EmailNotify/Test?to=alguem@exemplo.com
        public async Task<IActionResult> Test(string to)
        {
            if (string.IsNullOrWhiteSpace(to)) return Content("Passe ?to=email");
            await _email.SendAsync(to, "Teste CondoSphere", "<b>Funcionou!</b> Notificações por email ativas.");
            return Content($"Enviado para {to}");
        }
    }
}

using Azure.Core;
using CondoSphere.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace CondoSphere.API
{
    [ApiController]
    [Route("api/webhooks/stripe")]
    [Consumes("application/json")]
    [AllowAnonymous]
    public class StripeWebhookController : ControllerBase
    {
        private readonly IPaymentService _payments;
        private readonly ILogger<StripeWebhookController> _logger;

        public StripeWebhookController(IPaymentService payments, ILogger<StripeWebhookController> logger)
        {
            _payments = payments;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Receive()
        {
            try
            {
                // habilita ler o body mais de uma vez (precaução)
                HttpContext.Request.EnableBuffering();

                using var reader = new StreamReader(HttpContext.Request.Body, leaveOpen: true);
                var json = await reader.ReadToEndAsync();
                HttpContext.Request.Body.Position = 0;

                StringValues sigHeader = Request.Headers["Stripe-Signature"];
                if (StringValues.IsNullOrEmpty(sigHeader))
                    return BadRequest("Missing Stripe-Signature header.");

                await _payments.HandleWebhookAsync(json, sigHeader.ToString());
                return Ok(); // 2xx: Stripe considera entregue com sucesso
            }
            catch (Stripe.StripeException sex)
            {
                // assinatura inválida / evento malformado
                _logger.LogWarning(sex, "Stripe webhook error: {Message}", sex.Message);
                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error handling Stripe webhook");
                // devolver 200 evita múltiplas reentregas se já tratámos internamente;
                // aqui preferimos 500 para reentrega (depende da tua estratégia)
                return StatusCode(500);
            }
        }
    }
}

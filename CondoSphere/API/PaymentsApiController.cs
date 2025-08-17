using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using CondoSphere.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CondoSphere.API
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsApiController : ControllerBase
    {
        private readonly IPaymentService _payments;
        private readonly IConfiguration _cfg;
        public PaymentsApiController(IPaymentService payments, IConfiguration cfg)
        {
            _payments = payments;
            _cfg = cfg;
        }

        public class CreateReq { public int QuotaId { get; set; } }

        [HttpPost("card/intent")]
        public async Task<IActionResult> CreateCardIntent([FromBody] CreateReq req)
        {
            var (clientSecret, intentId) = await _payments.CreateCardIntentAsync(req.QuotaId);
            return Ok(new
            {
                clientSecret,
                intentId,
                publishableKey = _cfg["Stripe:PublishableKey"]
            });
        }

        [HttpPost("multibanco/create")]
        public async Task<IActionResult> CreateMultibanco([FromBody] CreateReq req)
        {
            var (intentId, reference, expiresAt) = await _payments.CreateMultibancoAsync(req.QuotaId);
            return Ok(new { intentId, reference, expiresAt });
        }

        // Webhook precisa ser público e sem auth
        [AllowAnonymous]
        [HttpPost("stripe-webhook")]
        public async Task<IActionResult> StripeWebhook()
        {
            using var reader = new StreamReader(Request.Body);
            var json = await reader.ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"].ToString();

            await _payments.HandleWebhookAsync(json, signature);
            return Ok(); // 200 para Stripe
        }
    }
}

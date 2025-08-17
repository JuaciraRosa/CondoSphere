using Azure.Core;
using CondoSphere.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CondoSphere.API
{
    [ApiController]
    [Route("api/webhooks/stripe")]
    [AllowAnonymous]
    public class StripeWebhookController : ControllerBase
    {
        private readonly IPaymentService _payments;
        public StripeWebhookController(IPaymentService payments) => _payments = payments;

        [HttpPost]
        public async Task<IActionResult> Receive()
        {
            using var reader = new StreamReader(HttpContext.Request.Body);
            var json = await reader.ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"];
            await _payments.HandleWebhookAsync(json, signature);
            return Ok();
        }
    }
}

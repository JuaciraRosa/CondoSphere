using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using CondoSphere.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.API
{
  
    [ApiController]
    [Route("api/payments")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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

        [HttpPost("card/intent")]
        public async Task<IActionResult> CreateCardIntent([FromBody] CreateReq req)
        {
            try
            {
                var cfg = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                var pk = cfg["Stripe:PublishableKey"];
                var sk = cfg["Stripe:SecretKey"];
                if (string.IsNullOrWhiteSpace(pk) || string.IsNullOrWhiteSpace(sk))
                    return BadRequest(new { error = "Stripe keys not configured. Set Stripe:PublishableKey and Stripe:SecretKey." });

                var (clientSecret, intentId) = await _payments.CreateCardIntentAsync(req.QuotaId);
                return Ok(new { clientSecret, intentId, publishableKey = pk });
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { error = ex.InnerException?.Message ?? ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }



        // API/PaymentsApiController.cs
        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm([FromBody] ConfirmReq req)
        {
            if (string.IsNullOrWhiteSpace(req.IntentId))
                return BadRequest(new { error = "intentId is required" });

            var status = await _payments.ConfirmAndMarkAsync(req.IntentId);
            return Ok(new { status });
        }

        public class ConfirmReq { public string IntentId { get; set; } }




    }

}

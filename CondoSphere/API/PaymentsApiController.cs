using CondoSphere.API.Models;
using CondoSphere.Data;
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
    [AllowAnonymous] 
    public class PaymentsApiController : ControllerBase
    {
        private readonly IPaymentService _payments;
        private readonly IConfiguration _cfg;
        public PaymentsApiController(IPaymentService payments, IConfiguration cfg)
        { _payments = payments; _cfg = cfg; }

      
        public class ConfirmReq { public string IntentId { get; set; } = ""; }

        // Webhook já era público
        [HttpPost("stripe-webhook")]
        public async Task<IActionResult> StripeWebhook()
        {
            using var reader = new StreamReader(Request.Body);
            var json = await reader.ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"].ToString();
            await _payments.HandleWebhookAsync(json, signature);
            return Ok();
        }


        [HttpPost("card/intent")]
        public async Task<IActionResult> CreateCardIntent([FromBody] CreateReq req,
                                                  [FromServices] ApplicationDbContext db)
        {
            try
            {
                var pk = _cfg["Stripe:PublishableKey"];
                var sk = _cfg["Stripe:SecretKey"];
                if (string.IsNullOrWhiteSpace(pk) || string.IsNullOrWhiteSpace(sk))
                    return BadRequest(new { error = "Stripe keys not configured. Set Stripe:PublishableKey and Stripe:SecretKey." });

                // 1) cria o intent (como você já faz)
                var (clientSecret, intentId) = await _payments.CreateCardIntentAsync(req.QuotaId);

                // 2) Garante um Payment Pendente + grava o e-mail do pagador
                var quota = await db.Quotas.AsNoTracking().FirstOrDefaultAsync(q => q.Id == req.QuotaId);
                if (quota == null) return NotFound(new { error = "Quota not found" });

                // tenta pegar email do corpo; se vier vazio, tenta do usuário logado
                var email = (req.Email ?? "").Trim();
                if (string.IsNullOrEmpty(email) && User?.Identity?.IsAuthenticated == true)
                {
                    // tente de um claim padrão
                    email = User.Claims.FirstOrDefault(c =>
                                c.Type == "email" ||
                                c.Type.EndsWith("/email", StringComparison.OrdinalIgnoreCase))?.Value ?? "";
                }

                var payment = await db.Payments
                    .FirstOrDefaultAsync(p => p.QuotaId == req.QuotaId && p.Status != PaymentStatusType.Succeeded);

                if (payment == null)
                {
                    payment = new Payment
                    {
                        QuotaId = req.QuotaId,
                        Amount = quota.Amount,
                        Method = PaymentMethodType.Card,
                        Status = PaymentStatusType.Pending,
                        Provider = "stripe",
                        ProviderPaymentId = intentId,
                        CreatedAt = DateTime.UtcNow,
                        PayerEmail = string.IsNullOrWhiteSpace(email) ? quota.DebtorEmail : email
                    };
                    db.Payments.Add(payment);
                }
                else
                {
                    payment.ProviderPaymentId = intentId;
                    payment.Status = PaymentStatusType.Pending;
                    if (!string.IsNullOrWhiteSpace(email))
                        payment.PayerEmail = email;
                    else if (string.IsNullOrWhiteSpace(payment.PayerEmail))
                        payment.PayerEmail = quota.DebtorEmail; // fallback
                }

                await db.SaveChangesAsync();

                return Ok(new { clientSecret, intentId, publishableKey = pk });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { error = ex.InnerException?.Message ?? ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, detail = ex.ToString() });
            }
        }

        //[HttpPost("card/intent")]
        //public async Task<IActionResult> CreateCardIntent([FromBody] CreateReq req)
        //{
        //    try
        //    {
        //        var pk = _cfg["Stripe:PublishableKey"];
        //        var sk = _cfg["Stripe:SecretKey"];
        //        if (string.IsNullOrWhiteSpace(pk) || string.IsNullOrWhiteSpace(sk))
        //            return BadRequest(new { error = "Stripe keys not configured. Set Stripe:PublishableKey and Stripe:SecretKey." });

        //        var (clientSecret, intentId) = await _payments.CreateCardIntentAsync(req.QuotaId);
        //        return Ok(new { clientSecret, intentId, publishableKey = pk });
        //    }
        //    catch (DbUpdateException ex)
        //    {
        //        return StatusCode(500, new { error = ex.InnerException?.Message ?? ex.Message });
        //    }
        //    catch (Exception ex)
        //    {
        //        // IMPORTANTE: devolve JSON; nada de HTML
        //        return StatusCode(500, new { error = ex.Message, detail = ex.ToString() });
        //    }
        //}

        //[HttpPost("confirm")]
        //public async Task<IActionResult> Confirm([FromBody] ConfirmReq req)
        //{
        //    if (string.IsNullOrWhiteSpace(req.IntentId))
        //        return BadRequest(new { error = "intentId is required" });

        //    var status = await _payments.ConfirmAndMarkAsync(req.IntentId);
        //    return Ok(new { status });
        //}

        //[HttpPost("confirm")]
        //public async Task<IActionResult> Confirm([FromBody] ConfirmReq req,
        //                                   [FromServices] ApplicationDbContext db)
        //{
        //    if (string.IsNullOrWhiteSpace(req.IntentId))
        //        return BadRequest(new { error = "intentId is required" });

        //    // Se você já chama o serviço Stripe, mantenha:
        //    await _payments.ConfirmAndMarkAsync(req.IntentId);

        //    // Marca o pagamento como pendente (aguardando avaliação do gestor)
        //    var pay = await db.Payments.FirstOrDefaultAsync(p => p.ProviderPaymentId == req.IntentId);
        //    if (pay == null) return NotFound(new { error = "Payment not found" });

        //    if (pay.Status != PaymentStatusType.Canceled &&
        //        pay.Status != PaymentStatusType.Failed &&
        //        pay.Status != PaymentStatusType.Succeeded) 
        //    {
        //        pay.Status = PaymentStatusType.Pending;
        //        pay.PaidAt = null;
        //    }

        //    // Garante que a quota não está marcada como paga aqui
        //    var quota = await db.Quotas.FirstOrDefaultAsync(q => q.Id == pay.QuotaId);
        //    if (quota != null) quota.IsPaid = false;

        //    await db.SaveChangesAsync();

        //    return Ok(new { status = "Pending", quotaId = pay.QuotaId });
        //}

        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm([FromBody] ConfirmReq req,
                                         [FromServices] ApplicationDbContext db)
        {
            if (string.IsNullOrWhiteSpace(req.IntentId))
                return BadRequest(new { error = "intentId is required" });

            await _payments.ConfirmAndMarkAsync(req.IntentId);

            var pay = await db.Payments.FirstOrDefaultAsync(p => p.ProviderPaymentId == req.IntentId);
            if (pay == null) return NotFound(new { error = "Payment not found" });

            // mantém como pendente até o gestor aprovar
            if (pay.Status is not PaymentStatusType.Canceled and not PaymentStatusType.Failed and not PaymentStatusType.Succeeded)
            {
                pay.Status = PaymentStatusType.Pending;
                pay.PaidAt = null;
            }

            // Garante isPaid = false aqui
            var quota = await db.Quotas.FirstOrDefaultAsync(q => q.Id == pay.QuotaId);
            if (quota != null) quota.IsPaid = false;

            await db.SaveChangesAsync();

            return Ok(new { status = "Pending", quotaId = pay.QuotaId });
        }



    }

}

using CondoSphere.Data;
using CondoSphere.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace CondoSphere.Services
{
    public class PaymentServiceStripe : IPaymentService
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _cfg;

        // Event type strings (evita depender de Stripe.Events.*)
        private const string PI_SUCCEEDED = "payment_intent.succeeded";
        private const string PI_FAILED = "payment_intent.payment_failed";
        private const string PI_CANCELED = "payment_intent.canceled";

        public PaymentServiceStripe(ApplicationDbContext db, IConfiguration cfg)
        {
            _db = db;
            _cfg = cfg;
            StripeConfiguration.ApiKey = _cfg["Stripe:SecretKey"];
        }

        public async Task<(string clientSecret, string paymentIntentId)> CreateCardIntentAsync(int quotaId)
        {
            var quota = await _db.Quotas
                .Include(q => q.Unit).ThenInclude(u => u.Condominium)
                .FirstOrDefaultAsync(q => q.Id == quotaId);
            if (quota == null) throw new Exception("Quota not found.");
            if (quota.IsPaid) throw new Exception("Quota already paid.");

            // 1) Já existe payment para esta quota?
            var existing = await _db.Payments.FirstOrDefaultAsync(p => p.QuotaId == quotaId);

            // 2) Se existir e estiver pendente com PaymentIntent válido, reusa-o
            if (existing != null && existing.Status != PaymentStatusType.Succeeded &&
                !string.IsNullOrEmpty(existing.ProviderPaymentId))
            {
                var piService = new PaymentIntentService();
                var pi = await piService.GetAsync(existing.ProviderPaymentId);
                if (pi != null && !string.Equals(pi.Status, "canceled", StringComparison.OrdinalIgnoreCase))
                    return (pi.ClientSecret, pi.Id);
                // se foi cancelado, vamos criar outro e atualizar a mesma linha
            }

            // 3) Criar/atualizar PaymentIntent
            var amountCents = (long)(quota.Amount * 100m);
            var options = new PaymentIntentCreateOptions
            {
                Amount = amountCents,
                Currency = "eur",
                PaymentMethodTypes = new List<string> { "card" },
                Description = $"Quota {quotaId} - {quota.DueDate:yyyy-MM}",
                Metadata = new Dictionary<string, string> { ["quota_id"] = quotaId.ToString() }
            };

            var piServiceNew = new PaymentIntentService();
            var intent = await piServiceNew.CreateAsync(
                options,
                // chave de idempotência extra: evita duplicar intents no Stripe em cliques repetidos
                new RequestOptions { IdempotencyKey = $"quota-{quotaId}-card" }
            );

            if (existing == null)
            {
                _db.Payments.Add(new Payment
                {
                    QuotaId = quotaId,
                    Amount = quota.Amount,
                    Method = PaymentMethodType.Card,
                    Status = PaymentStatusType.Pending,
                    Provider = "stripe",
                    ProviderPaymentId = intent.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                // Atualiza a MESMA linha para não bater no índice único
                existing.Amount = quota.Amount;
                existing.Method = PaymentMethodType.Card;
                existing.Status = PaymentStatusType.Pending;
                existing.Provider = "stripe";
                existing.ProviderPaymentId = intent.Id;
                existing.CreatedAt = DateTime.UtcNow;
                _db.Payments.Update(existing);
            }

            await _db.SaveChangesAsync();
            return (intent.ClientSecret, intent.Id);
        }



        public async Task HandleWebhookAsync(string json, string signatureHeader)
        {
            var secret = _cfg["Stripe:WebhookSecret"];
            var stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, secret);

            // Trabalhamos com literais para evitar dependência de Stripe.Events.*
            if (stripeEvent.Type == "payment_intent.succeeded" ||
                stripeEvent.Type == "payment_intent.payment_failed" ||
                stripeEvent.Type == "payment_intent.canceled")
            {
                var pi = stripeEvent.Data.Object as PaymentIntent;
                if (pi == null) return;

                // Recarrega expandindo latest_charge para obter ReceiptUrl
                var piService = new PaymentIntentService();
                pi = await piService.GetAsync(pi.Id, new PaymentIntentGetOptions
                {
                    Expand = new List<string> { "latest_charge" }
                });

                var payment = await _db.Payments.FirstOrDefaultAsync(p => p.ProviderPaymentId == pi.Id);
                if (payment == null) return;

                if (stripeEvent.Type == "payment_intent.succeeded")
                {
                    payment.Status = PaymentStatusType.Succeeded;
                    payment.PaidAt = DateTime.UtcNow;
                    payment.ReceiptUrl = pi.LatestCharge?.ReceiptUrl;

                    if (pi.Metadata != null &&
                        pi.Metadata.TryGetValue("quota_id", out var qid) &&
                        int.TryParse(qid, out var quotaId))
                    {
                        var quota = await _db.Quotas.FindAsync(quotaId);
                        if (quota != null) quota.IsPaid = true;
                    }
                }
                else if (stripeEvent.Type == "payment_intent.payment_failed")
                {
                    payment.Status = PaymentStatusType.Failed;
                }
                else if (stripeEvent.Type == "payment_intent.canceled")
                {
                    payment.Status = PaymentStatusType.Canceled;
                }

                await _db.SaveChangesAsync();
            }
            // Outros eventos podem ser ignorados ou tratados aqui conforme necessário
        }



        public async Task<string> ConfirmAndMarkAsync(string intentId)
        {
            var piService = new PaymentIntentService();
            var pi = await piService.GetAsync(intentId); // sem expands

            if (string.Equals(pi.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
            {
                var payment = await _db.Payments
                    .Include(p => p.Quota)
                    .FirstOrDefaultAsync(p => p.ProviderPaymentId == pi.Id);

                if (payment != null)
                {
                    payment.Status = PaymentStatusType.Succeeded;
                    payment.PaidAt = DateTime.UtcNow;

                    // Se fizeres questão do recibo via Charge, podemos tentar depois.
                    // payment.ReceiptUrl = ... (opcional)

                    if (payment.Quota != null)
                        payment.Quota.IsPaid = true;

                    await _db.SaveChangesAsync();
                }
            }

            return pi.Status; // succeeded / processing / requires_action / canceled / ...
        }
    }
}

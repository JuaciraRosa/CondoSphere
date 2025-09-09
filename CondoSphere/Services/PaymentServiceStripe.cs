using CondoSphere.Data;
using CondoSphere.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace CondoSphere.Services
{
    public class PaymentServiceStripe : IPaymentService
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _cfg;

        public PaymentServiceStripe(ApplicationDbContext db, IConfiguration cfg)
        {
            _db = db;
            _cfg = cfg;
            StripeConfiguration.ApiKey = _cfg["Stripe:SecretKey"];
        }

        // ---------- 1) Checkout Session (sem duplicar session_id) ----------
        public async Task<string> CreateCheckoutSessionForQuotaAsync(int quotaId, string successUrl, string cancelUrl)
        {
            var quota = await _db.Quotas
                .Include(q => q.Unit)
                .FirstOrDefaultAsync(q => q.Id == quotaId)
                ?? throw new InvalidOperationException("Quota não encontrada.");

            // IMPORTANTE: manter o placeholder literal, sem codificar!
            var successUrlWithParam = successUrl + (successUrl.Contains("?") ? "&" : "?") + "session_id={CHECKOUT_SESSION_ID}";

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = successUrlWithParam,
                CancelUrl = cancelUrl,
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
        {
            new()
            {
                Quantity = 1,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmountDecimal = quota.Amount * 100m,
                    Currency = "eur",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = $"Quota #{quota.Id} - Unidade {quota.Unit?.Number}"
                    }
                }
            }
        },
                // ConfirmAndMarkAsync vai usar este metadata
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["QuotaId"] = quota.Id.ToString()
                    }
                }
            };

            var sSrv = new SessionService();
            var session = await sSrv.CreateAsync(options);
            return session.Url!;
        }

        // ---------- 2) Confirma e marca pago usando o PaymentIntentId ----------
        public async Task<string> ConfirmAndMarkAsync(string paymentIntentId)
        {
            var piService = new PaymentIntentService();
            var intent = await piService.GetAsync(paymentIntentId);

            if (intent == null || !string.Equals(intent.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("PaymentIntent não está pago.");

            if (!intent.Metadata.TryGetValue("QuotaId", out var quotaIdStr) ||
                !int.TryParse(quotaIdStr, out var quotaId))
                throw new InvalidOperationException("Metadata 'QuotaId' não encontrado no PaymentIntent.");

            var quota = await _db.Quotas
                .Include(q => q.Payment)
                .FirstOrDefaultAsync(q => q.Id == quotaId)
                ?? throw new InvalidOperationException("Quota não encontrada.");

            // evita duplicar
            if (quota.Payment != null)
                return quota.Payment.Id.ToString();

            // montante (Stripe devolve em cêntimos)
            long cents = intent.AmountReceived != 0 ? intent.AmountReceived : intent.Amount;
            var amount = cents / 100m;

            // tenta obter Charge para ReceiptUrl
            string? receiptUrl = null;
            string? chargeId = null;
            try
            {
                var chargeSrv = new ChargeService();
                var list = await chargeSrv.ListAsync(new ChargeListOptions
                {
                    PaymentIntent = intent.Id,
                    Limit = 1
                });
                var charge = list?.Data?.FirstOrDefault();
                receiptUrl = charge?.ReceiptUrl;
                chargeId = charge?.Id;
            }
            catch { /* opcional: log */ }

            var payment = new Payment
            {
                QuotaId = quota.Id,
                Amount = amount,
                Method = PaymentMethodType.Card,
                Status = PaymentStatusType.Succeeded,
                Provider = "stripe",
                ProviderPaymentId = intent.Id,   // PaymentIntent Id
                ProviderReference = chargeId,    // Charge Id (se houver)
                ReceiptUrl = receiptUrl,
                CreatedAt = DateTime.UtcNow,
                PaidAt = DateTime.UtcNow
            };

            _db.Payments.Add(payment);
            quota.IsPaid = true;
            quota.Payment = payment;

            await _db.SaveChangesAsync();
            return payment.Id.ToString();
        }

        // ---------- 3) Webhook (opcional; útil em produção) ----------
        public async Task HandleWebhookAsync(string json, string signatureHeader)
        {
            var secret = _cfg["Stripe:WebhookSecret"];
            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("Stripe WebhookSecret not configured.");

            var stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, secret);

            if (stripeEvent.Type is "payment_intent.succeeded"
                                or "payment_intent.payment_failed"
                                or "payment_intent.canceled")
            {
                var evtPi = stripeEvent.Data.Object as PaymentIntent;
                if (evtPi == null) return;

                var piService = new PaymentIntentService();
                var pi = await piService.GetAsync(evtPi.Id, new PaymentIntentGetOptions
                {
                    Expand = new List<string> { "latest_charge" }
                });

                var payment = await _db.Payments
                    .Include(p => p.Quota)
                    .FirstOrDefaultAsync(p => p.ProviderPaymentId == pi.Id);

                if (payment == null) return;

                var changed = false;
                if (stripeEvent.Type == "payment_intent.succeeded")
                {
                    payment.Status = PaymentStatusType.Succeeded;
                    payment.PaidAt = DateTime.UtcNow;
                    payment.ReceiptUrl = pi.LatestCharge?.ReceiptUrl;
                    if (payment.Quota != null) payment.Quota.IsPaid = true;
                    changed = true;
                }
                else if (stripeEvent.Type == "payment_intent.payment_failed")
                {
                    if (payment.Status != PaymentStatusType.Failed)
                    {
                        payment.Status = PaymentStatusType.Failed;
                        changed = true;
                    }
                }
                else if (stripeEvent.Type == "payment_intent.canceled")
                {
                    if (payment.Status != PaymentStatusType.Canceled)
                    {
                        payment.Status = PaymentStatusType.Canceled;
                        changed = true;
                    }
                }

                if (changed)
                    await _db.SaveChangesAsync();
            }


        }

        public async Task<(string clientSecret, string paymentIntentId)> CreateCardIntentAsync(int quotaId)
        {
            // carrega a quota
            var quota = await _db.Quotas
                .Include(q => q.Unit)
                .FirstOrDefaultAsync(q => q.Id == quotaId)
                ?? throw new InvalidOperationException("Quota não encontrada.");

            if (quota.IsPaid)
                throw new InvalidOperationException("Quota já está paga.");

            // verifica se já existe um Payment pendente para esta quota
            var existing = await _db.Payments.FirstOrDefaultAsync(p => p.QuotaId == quotaId);

            // cria PaymentIntent (card elements)
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(quota.Amount * 100m),
                Currency = "eur",
                PaymentMethodTypes = new List<string> { "card" },
                Description = $"Quota {quota.Id} - {quota.DueDate:yyyy-MM}",
                Metadata = new Dictionary<string, string>
                {
                    ["QuotaId"] = quota.Id.ToString()
                }
            };

            var piSrv = new PaymentIntentService();
            var intent = await piSrv.CreateAsync(options);

            // registra/atualiza o Payment localmente (pendente)
            if (existing == null)
            {
                _db.Payments.Add(new Payment
                {
                    QuotaId = quota.Id,
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
                existing.Amount = quota.Amount;
                existing.Method = PaymentMethodType.Card;
                existing.Status = PaymentStatusType.Pending;
                existing.Provider = "stripe";
                existing.ProviderPaymentId = intent.Id;
                existing.CreatedAt = DateTime.UtcNow;
                _db.Payments.Update(existing);
            }

            await _db.SaveChangesAsync();

            // devolve os dados esperados pelos controllers antigos
            return (intent.ClientSecret, intent.Id);
        }
    }


}




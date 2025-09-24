using CondoSphere.Data;
using CondoSphere.Messaging;
using CondoSphere.Models;
using Google.Apis.Calendar.v3.Data;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


namespace CondoSphere.Services
{
    public class PaymentServiceStripe : IPaymentService
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _cfg;
        private readonly IHttpContextAccessor _http;
        private readonly IEmailSender _email;
        private readonly ISystemSettingsService _settings;
        public PaymentServiceStripe(ApplicationDbContext db, IConfiguration cfg, IHttpContextAccessor http, IEmailSender email, ISystemSettingsService settings)
        {
            _db = db;
            _cfg = cfg;
            _http = http;
            _email = email;
            _settings = settings;
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

            var payerEmail = _http.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;


            var options = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = successUrlWithParam,
                CancelUrl = cancelUrl,
                CustomerEmail = string.IsNullOrWhiteSpace(payerEmail) ? null : payerEmail,
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
        //public async Task<string> ConfirmAndMarkAsync(string paymentIntentId)
        //{
        //    var piService = new PaymentIntentService();
        //    var intent = await piService.GetAsync(paymentIntentId);

        //    if (intent == null || !string.Equals(intent.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
        //        throw new InvalidOperationException("PaymentIntent não está pago.");

        //    if (!intent.Metadata.TryGetValue("QuotaId", out var quotaIdStr) ||
        //        !int.TryParse(quotaIdStr, out var quotaId))
        //        throw new InvalidOperationException("Metadata 'QuotaId' não encontrado no PaymentIntent.");

        //    var quota = await _db.Quotas
        //        .Include(q => q.Payment)
        //        .FirstOrDefaultAsync(q => q.Id == quotaId)
        //        ?? throw new InvalidOperationException("Quota não encontrada.");

        //    // evita duplicar
        //    if (quota.Payment != null)
        //        return quota.Payment.Id.ToString();

        //    // montante (Stripe devolve em cêntimos)
        //    long cents = intent.AmountReceived != 0 ? intent.AmountReceived : intent.Amount;
        //    var amount = cents / 100m;

        //    // tenta obter Charge para ReceiptUrl
        //    string? receiptUrl = null;
        //    string? chargeId = null;
        //    try
        //    {
        //        var chargeSrv = new ChargeService();
        //        var list = await chargeSrv.ListAsync(new ChargeListOptions
        //        {
        //            PaymentIntent = intent.Id,
        //            Limit = 1
        //        });
        //        var charge = list?.Data?.FirstOrDefault();
        //        receiptUrl = charge?.ReceiptUrl;
        //        chargeId = charge?.Id;
        //    }
        //    catch { /* opcional: log */ }

        //    var payment = new Payment
        //    {
        //        QuotaId = quota.Id,
        //        Amount = amount,
        //        Method = PaymentMethodType.Card,
        //        Status = PaymentStatusType.Succeeded,
        //        Provider = "stripe",
        //        ProviderPaymentId = intent.Id,   // PaymentIntent Id
        //        ProviderReference = chargeId,    // Charge Id (se houver)
        //        ReceiptUrl = receiptUrl,
        //        CreatedAt = DateTime.UtcNow,
        //        PaidAt = DateTime.UtcNow
        //    };

        //    _db.Payments.Add(payment);
        //    quota.IsPaid = true;
        //    quota.Payment = payment;

        //    await _db.SaveChangesAsync();
        //    await TrySendReceiptEmailAsync(payment.Id);
        //    return payment.Id.ToString();
        //}



        public async Task<string> ConfirmAndMarkAsync(string paymentIntentId)
        {
            // 1) Stripe: recuperar intent e receipt
            var piService = new PaymentIntentService();
            var intent = await piService.GetAsync(paymentIntentId);

            if (intent == null || !string.Equals(intent.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("PaymentIntent não está pago.");

            // 2) tentar obter Charge/ReceiptUrl
            string? receiptUrl = null;
            string? chargeId = null;
            try
            {
                var chargeSrv = new ChargeService();
                var list = await chargeSrv.ListAsync(new ChargeListOptions { PaymentIntent = intent.Id, Limit = 1 });
                var charge = list?.Data?.FirstOrDefault();
                receiptUrl = charge?.ReceiptUrl;
                chargeId = charge?.Id;
            }
            catch { /* opcional: log */ }

            // 3) Atualiza APENAS o Payment (já criado como Pending em CreateCardIntentAsync)
            var payment = await _db.Payments
                .Include(p => p.Quota)
                .FirstOrDefaultAsync(p => p.ProviderPaymentId == intent.Id);

            // Pode acontecer de não existir (ex.: fluxo fora do card intent). Cria, mas NÃO marca quota.
            if (payment == null)
            {
                // valor recebido em cêntimos
                long cents = intent.AmountReceived != 0 ? intent.AmountReceived : intent.Amount;
                var amount = cents / 100m;

                payment = new Payment
                {
                    QuotaId = intent.Metadata.TryGetValue("QuotaId", out var qid) && int.TryParse(qid, out var q)
                                ? q : 0,
                    Amount = amount,
                    Method = PaymentMethodType.Card,
                    Status = PaymentStatusType.Succeeded,
                    Provider = "stripe",
                    ProviderPaymentId = intent.Id,
                    ProviderReference = chargeId,
                    ReceiptUrl = receiptUrl,
                    CreatedAt = DateTime.UtcNow,
                    PaidAt = DateTime.UtcNow
                };
                _db.Payments.Add(payment);
            }
            else
            {
                payment.Status = PaymentStatusType.Succeeded;
                payment.PaidAt = DateTime.UtcNow;
                payment.ReceiptUrl = receiptUrl;
                payment.ProviderReference = chargeId;
                _db.Payments.Update(payment);
            }

            // 🚫 NÃO MARCAR A QUOTA AQUI
            // if (payment.Quota != null) payment.Quota.IsPaid = true;

            await _db.SaveChangesAsync();

            //  (só após aprovação do gestor)
            // await TrySendReceiptEmailAsync(payment.Id);

            return payment.Id.ToString();
        }



        private async Task TrySendReceiptEmailAsync(int paymentId)
        {
            var s = await _settings.GetCurrentAsync();
            if (!(s.EmailsEnabled && s.PaymentReceiptEmailEnabled))
                return;

            var payment = await _db.Payments
                .Include(p => p.Quota)
                    .ThenInclude(q => q.Unit)
                .FirstOrDefaultAsync(p => p.Id == paymentId);
            if (payment == null) return;

            // e-mail do utilizador autenticado (checkout self-service)
            var to = _http.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(to)) return;

            var userName = _http.HttpContext?.User?.Identity?.Name ?? to;

            var pt = CultureInfo.GetCultureInfo("pt-PT");
            var amountTx = payment.Amount.ToString("C2", pt);
            var paidAt = (payment.PaidAt ?? payment.CreatedAt).ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            var reference = !string.IsNullOrWhiteSpace(payment.ProviderReference)
                                ? payment.ProviderReference!
                                : payment.ProviderPaymentId;
            var method = payment.Method.ToString();

            // Link público (PDF) assinado e com expiração
            var invoiceUrl = MakePublicInvoiceUrl(payment.Id, TimeSpan.FromDays(7), pdf: true);

            var subject = s.PaymentReceiptEmailSubject ?? "Comprovativo de pagamento";

            var defaultHtml =
        $@"
<p>Olá {System.Net.WebUtility.HtmlEncode(userName)},</p>
<p>Recebemos o seu pagamento de <strong>{System.Net.WebUtility.HtmlEncode(amountTx)}</strong> em {paidAt}.</p>
<p>Referência: <code>{System.Net.WebUtility.HtmlEncode(reference)}</code> · Método: {System.Net.WebUtility.HtmlEncode(method)}</p>"
        + (string.IsNullOrWhiteSpace(payment.ReceiptUrl) ? "" :
           $@"<p>Recibo do provedor: <a href=""{payment.ReceiptUrl}"">{payment.ReceiptUrl}</a></p>")
        + $@"
<p>Pode consultar/guardar a fatura aqui: <a href=""{invoiceUrl}"">Ver fatura (PDF)</a></p>
<p>Cumprimentos,<br/>{System.Net.WebUtility.HtmlEncode(s.CompanyDisplayName ?? "CondoSphere")}</p>";

            var html = string.IsNullOrWhiteSpace(s.PaymentReceiptEmailHtml)
                ? defaultHtml
                : _settings.RenderTemplate(
                    s.PaymentReceiptEmailHtml,
                    new Dictionary<string, string>
                    {
                        ["User.FullName"] = userName,
                        ["User.Email"] = to,
                        ["Payment.Amount"] = amountTx,
                        ["Payment.Date"] = paidAt,
                        ["Payment.Reference"] = reference,
                        ["Payment.Method"] = method,
                        ["InvoiceUrl"] = invoiceUrl,
                        ["Company.Name"] = s.CompanyDisplayName ?? "CondoSphere",
                        ["Provider.ReceiptUrl"] = payment.ReceiptUrl ?? ""
                    });

            await _email.SendAsync(to, subject, html);
        }


        // ---------- 3) Webhook(opcional; útil em produção) ----------
        //public async Task HandleWebhookAsync(string json, string signatureHeader)
        //{
        //    var secret = _cfg["Stripe:WebhookSecret"];
        //    if (string.IsNullOrWhiteSpace(secret))
        //        throw new InvalidOperationException("Stripe WebhookSecret not configured.");

        //    var stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, secret);

        //    if (stripeEvent.Type is "payment_intent.succeeded"
        //                        or "payment_intent.payment_failed"
        //                        or "payment_intent.canceled")
        //    {
        //        var evtPi = stripeEvent.Data.Object as PaymentIntent;
        //        if (evtPi == null) return;

        //        var piService = new PaymentIntentService();
        //        var pi = await piService.GetAsync(evtPi.Id, new PaymentIntentGetOptions
        //        {
        //            Expand = new List<string> { "latest_charge" }
        //        });

        //        var payment = await _db.Payments
        //            .Include(p => p.Quota)
        //            .FirstOrDefaultAsync(p => p.ProviderPaymentId == pi.Id);

        //        if (payment == null) return;

        //        var changed = false;
        //        if (stripeEvent.Type == "payment_intent.succeeded")
        //        {
        //            payment.Status = PaymentStatusType.Succeeded;
        //            payment.PaidAt = DateTime.UtcNow;
        //            payment.ReceiptUrl = pi.LatestCharge?.ReceiptUrl;
        //            if (payment.Quota != null) payment.Quota.IsPaid = true;
        //            changed = true;
        //        }
        //        else if (stripeEvent.Type == "payment_intent.payment_failed")
        //        {
        //            if (payment.Status != PaymentStatusType.Failed)
        //            {
        //                payment.Status = PaymentStatusType.Failed;
        //                changed = true;
        //            }
        //        }
        //        else if (stripeEvent.Type == "payment_intent.canceled")
        //        {
        //            if (payment.Status != PaymentStatusType.Canceled)
        //            {
        //                payment.Status = PaymentStatusType.Canceled;
        //                changed = true;
        //            }
        //        }

        //        if (changed)
        //            await _db.SaveChangesAsync();
        //    }


        //}





        private string MakePublicInvoiceUrl(int paymentId, TimeSpan validFor, bool pdf = false)
        {
            // baseUrl a partir do request atual; se estiver null (ex.: job), usa App:BaseUrl
            var baseUrl =
                $"{_http.HttpContext?.Request?.Scheme}://{_http.HttpContext?.Request?.Host.Value}".TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
                baseUrl = _cfg["App:BaseUrl"]?.TrimEnd('/');

            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("Configure App:BaseUrl no appsettings quando não houver HttpContext.");

            var exp = DateTimeOffset.UtcNow.Add(validFor).ToUnixTimeSeconds();
            var payload = $"{paymentId}.{exp}";
            var sig = Base64Url(HmacSha256(Encoding.UTF8.GetBytes(GetInvoiceSecret()), payload));

            var action = pdf ? "PublicInvoicePdf" : "PublicInvoice";
            return $"{baseUrl}/Payments/{action}?id={paymentId}&exp={exp}&sig={sig}";
        }

        private string GetInvoiceSecret()
        {
            var secret = _cfg["InvoiceLinks:Secret"] ?? _cfg["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("Configure InvoiceLinks:Secret (ou Jwt:Key) para links públicos de fatura.");
            return secret!;
        }

        private static byte[] HmacSha256(byte[] key, string data)
        {
            using var h = new HMACSHA256(key);
            return h.ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        private static string Base64Url(byte[] data) =>
            Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

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

        public async Task HandleWebhookAsync(string json, string signatureHeader)
        {
            var secret = _cfg["Stripe:WebhookSecret"];
            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("Stripe WebhookSecret not configured.");

            var stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, secret);

            // CORRIGIDO: removido ')' extra e ';' no final da linha
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

                    // REMOVIDO: não marcar a quota aqui (validação ficará na action Approve)
                    // if (payment.Quota != null) payment.Quota.IsPaid = true;

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

    }



}




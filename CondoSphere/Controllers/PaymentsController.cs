



using CondoSphere.Data.Interfaces;
using CondoSphere.Messaging;
using CondoSphere.Models;
using CondoSphere.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.IdentityModel.Claims;
using System.Security.Cryptography;
using System.Text;
using PdfUnit = QuestPDF.Infrastructure.Unit;

namespace CondoSphere.Controllers
{
   

    [Authorize(Roles = "Administrator,Manager,Resident")]
    public class PaymentsController : Controller
    {
        private readonly IPaymentRepository _payments;
        private readonly IQuotaRepository _quotas;
        private readonly IPaymentService _paymentService;
        private readonly IConfiguration _cfg;
        private readonly DomainNotificationService _notify;
        private readonly ISystemSettingsService _settings;
        private readonly IEmailSender _email;



        public PaymentsController(
            IPaymentRepository payments,
            IQuotaRepository quotas,
            IPaymentService paymentService,
            IConfiguration cfg,
            DomainNotificationService notify,
              ISystemSettingsService settings,   
    IEmailSender email)           
        {
            _payments = payments;
            _quotas = quotas;
            _paymentService = paymentService;
            _cfg = cfg;
            _notify = notify;
            _settings = settings;              
            _email = email;
        }

        // ===== Stripe AJAX =====
        public class CreateReq { public int QuotaId { get; set; } }

        [HttpPost("/payments/card/intent")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CardIntent([FromBody] CreateReq req)
        {
            var (clientSecret, intentId) = await _paymentService.CreateCardIntentAsync(req.QuotaId);
            return Ok(new
            {
                clientSecret,
                intentId,
                publishableKey = _cfg["Stripe:PublishableKey"]
            });
        }

        public async Task<IActionResult> Index()
        {
            var list = await _payments.GetAllDetailedAsync();
            return View(list);
        }

        public async Task<IActionResult> Details(int id)
        {
            var payment = await _payments.GetByIdDetailedAsync(id);
            if (payment == null) return NotFound();
            return View(payment);
        }


        // GET: Payments/Delete/{id}
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var payment = await _payments.GetByIdDetailedAsync(id);
            if (payment == null) return NotFound();
            return View(payment);
        }

        // POST: Payments/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _payments.DeleteAsync(id);
                TempData["Success"] = "Payment deleted successfully.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Payment could not be deleted due to related records.";
            }
            catch
            {
                TempData["Error"] = "An unexpected error occurred while deleting the payment.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ------- helpers -------
        private async Task LoadSelects(Payment? current = null)
        {
            var quotas = await _quotas.GetAllAsync();
            var items = quotas.Select(q => new SelectListItem
            {
                Value = q.Id.ToString(),
                Text = $"#{q.Id} — {q.DueDate:yyyy-MM} — {q.Amount:N2}"
            }).ToList();

            ViewBag.QuotaId = new SelectList(items, "Value", "Text", current?.QuotaId);
            ViewBag.MethodList = new SelectList(Enum.GetValues(typeof(PaymentMethodType)));
            ViewBag.StatusList = new SelectList(Enum.GetValues(typeof(PaymentStatusType)));
        }

        private string ResolveDestEmail(Payment payment)
        {
            // Se o Payment tiver um campo de e-mail do pagador, usa aqui:
            // if (!string.IsNullOrWhiteSpace(payment.PayerEmail)) return payment.PayerEmail;

            // fallback: email do utilizador autenticado
            var claim = User.FindFirst(ClaimTypes.Email) ?? User.FindFirst(ClaimTypes.Name);
            if (claim != null && !string.IsNullOrWhiteSpace(claim.Value))
                return claim.Value;

            // último recurso (teste)
            return "Support@condosphere-web-app.somee.com";
        }



        private async Task SendPaymentReceiptAsync(int paymentId)
        {
            // 1) Respeita os “switches” do painel
            var s = await _settings.GetCurrentAsync();
            if (!(s.EmailsEnabled && s.PaymentReceiptEmailEnabled)) return;

            // 2) Carrega o pagamento
            var payment = await _payments.GetByIdDetailedAsync(paymentId);
            if (payment == null) return;

            // 3) Destinatário do e-mail (sem navegar por Unit/Resident/Owner)
            var to = ResolveDestEmail(payment);
            if (string.IsNullOrWhiteSpace(to)) return;

            var displayName = User?.Identity?.Name;
            var userName = !string.IsNullOrWhiteSpace(displayName) ? displayName! : to;

            // 4) Dados do comprovativo
            var culture = System.Globalization.CultureInfo.GetCultureInfo("pt-PT");
            var amountText = payment.Amount.ToString("C2", culture);
            var paidAtDt = (payment.PaidAt ?? payment.CreatedAt).ToLocalTime();
            var paidAt = paidAtDt.ToString("yyyy-MM-dd HH:mm");

            var reference = !string.IsNullOrWhiteSpace(payment.ProviderReference)
                                ? payment.ProviderReference!
                                : payment.ProviderPaymentId;

            var methodName = payment.Method.ToString();

            // Link da fatura (a mesma do módulo Payments)
            var invoiceUrl = MakePublicInvoiceUrl(payment.Id, TimeSpan.FromDays(7), pdf: true);

            var subject = s.PaymentReceiptEmailSubject ?? "Comprovativo de pagamento";

            // 5) Corpo (fallback) + ReceiptUrl do provedor (se existir)
            var defaultHtml = $@"
<p>Olá {System.Net.WebUtility.HtmlEncode(userName)},</p>
<p>Recebemos o seu pagamento de <strong>{System.Net.WebUtility.HtmlEncode(amountText)}</strong> em {paidAt}.</p>
<p>Referência: <code>{System.Net.WebUtility.HtmlEncode(reference)}</code> · Método: {System.Net.WebUtility.HtmlEncode(methodName)}</p>"
            + (string.IsNullOrWhiteSpace(payment.ReceiptUrl) ? "" :
               $@"<p>Recibo do provedor: <a href=""{payment.ReceiptUrl}"">{payment.ReceiptUrl}</a></p>")
            + $@"
<p>Pode consultar/guardar a fatura aqui: <a href=""{invoiceUrl}"">Ver fatura</a></p>
<p>Cumprimentos,<br/>{System.Net.WebUtility.HtmlEncode(s.CompanyDisplayName ?? "CondoSphere")}</p>";

            // 6) Template configurável (se houver)
            var html = string.IsNullOrWhiteSpace(s.PaymentReceiptEmailHtml)
                ? defaultHtml
                : _settings.RenderTemplate(
                    s.PaymentReceiptEmailHtml,
                    new Dictionary<string, string>
                    {
                        ["User.FullName"] = userName,
                        ["User.Email"] = to,
                        ["Payment.Amount"] = amountText,
                        ["Payment.Date"] = paidAt,
                        ["Payment.Reference"] = reference,
                        ["Payment.Method"] = methodName,
                        ["InvoiceUrl"] = invoiceUrl,
                        ["Company.Name"] = s.CompanyDisplayName ?? "CondoSphere",
                        ["Provider.ReceiptUrl"] = payment.ReceiptUrl ?? ""
                    });

            await _email.SendAsync(to, subject, html);
        }



        [HttpPost]
        [Authorize(Roles = "Administrator,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendReceipt(int id)
        {
            // reutilize a mesma lógica do envio (ou extraia para um método privado)
            await SendPaymentReceiptAsync(id);
            TempData["Success"] = "Comprovativo reenviado.";
            return RedirectToAction(nameof(Details), new { id });
        }


        [HttpGet]
        public async Task<IActionResult> Invoice(int id)
        {
            var payment = await _payments.GetByIdDetailedAsync(id);
            if (payment == null) return NotFound();

            return View("Invoice", payment); // Views/Payments/Invoice.cshtml
        }



        // === PUBLIC INVOICE LINK (HMAC + expiração) ===
        private string GetInvoiceSecret()
        {
            var secret = _cfg["InvoiceLinks:Secret"] ?? _cfg["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("InvoiceLinks:Secret/Jwt:Key is not configured.");
            return secret!;
        }

        private static string Base64Url(byte[] data)
            => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static byte[] HmacSha256(byte[] key, string data)
        {
            using var h = new HMACSHA256(key);
            return h.ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            if (a.Length != b.Length) return false;
            var res = 0;
            for (int i = 0; i < a.Length; i++) res |= a[i] ^ b[i];
            return res == 0;
        }

        /// Gera URL pública com assinatura e expiração.
        /// pdf=false => /Payments/PublicInvoice (HTML)
        /// pdf=true  => /Payments/PublicInvoicePdf (PDF)
        private string MakePublicInvoiceUrl(int paymentId, TimeSpan validFor, bool pdf = false)
        {
            var secret = Encoding.UTF8.GetBytes(GetInvoiceSecret());
            var exp = DateTimeOffset.UtcNow.Add(validFor).ToUnixTimeSeconds();
            var payload = $"{paymentId}.{exp}";
            var sig = Base64Url(HmacSha256(secret, payload));
            var actionName = pdf ? "PublicInvoicePdf" : "PublicInvoice";
            return Url.Action(actionName, "Payments", new { id = paymentId, exp, sig }, protocol: Request.Scheme)!;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> PublicInvoice(int id, long exp, string sig)
        {
            // 1) expiração
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (exp <= now) return Unauthorized("Link expirado.");

            // 2) assinatura (id.exp com HMAC do secret)
            var secret = Encoding.UTF8.GetBytes(GetInvoiceSecret());
            var payload = $"{id}.{exp}";
            var expectedSig = Base64Url(HmacSha256(secret, payload));
            if (!FixedTimeEquals(expectedSig, sig ?? "")) return Unauthorized("Assinatura inválida.");

            // 3) carrega e renderiza a mesma view de fatura
            var payment = await _payments.GetByIdDetailedAsync(id);
            if (payment == null) return NotFound();

            // pode ser HTML (Invoice.cshtml) ou PDF se você trocar a implementação depois
            return View("Invoice", payment);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> PublicInvoicePdf(int id, long exp, string sig)
        {
            // 1) expiração
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (exp <= now) return Unauthorized("Link expirado.");

            // 2) assinatura
            var secret = Encoding.UTF8.GetBytes(GetInvoiceSecret());
            var payload = $"{id}.{exp}";
            var expectedSig = Base64Url(HmacSha256(secret, payload));
            if (!FixedTimeEquals(expectedSig, sig ?? "")) return Unauthorized("Assinatura inválida.");

            // 3) dados
            var payment = await _payments.GetByIdDetailedAsync(id);
            if (payment == null) return NotFound();

            var settings = await _settings.GetCurrentAsync();
            var pt = CultureInfo.GetCultureInfo("pt-PT");

            var company = settings.CompanyDisplayName ?? "CondoSphere";
            var amount = payment.Amount;
            var amountTx = amount.ToString("C2", pt);
            var paidAt = (payment.PaidAt ?? payment.CreatedAt).ToLocalTime();
            var paidAtTx = paidAt.ToString("yyyy-MM-dd HH:mm");
            var reference = string.IsNullOrWhiteSpace(payment.ProviderReference) ? payment.ProviderPaymentId : payment.ProviderReference!;
            var method = payment.Method.ToString();
            var compTx = payment.Quota?.DueDate.ToString("yyyy-MM") ?? "-";  // competência (ano-mês)
            var provUrl = payment.ReceiptUrl;

            // 4) PDF (QuestPDF)
            var pdfBytes = Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, PdfUnit.Centimetre);
                    page.DefaultTextStyle(t => t.FontSize(11));

                    // Cabeçalho com faixa e badge "PAGO"
                    page.Header().Element(header =>
                    {
                        header.Column(col =>
                        {
                            col.Spacing(6);

                            // faixinha colorida
                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Background(Colors.Indigo.Lighten4).Height(4);
                            });

                            // linha com nome da empresa e nº da fatura + badge
                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text(company).SemiBold().FontSize(18);

                                r.ConstantItem(220).AlignRight().Column(c2 =>
                                {
                                    c2.Item().Text($"Fatura #{payment.Id}").SemiBold();
                                    r.ConstantItem(220).AlignRight().Column(c2 =>
                                    {
                                        // linha de cima: "Fatura #"
                                        c2.Item().AlignRight().Text($"Fatura #{payment.Id}").SemiBold();

                                        // linha de baixo: badge PAGO
                                        c2.Item()
                                          .AlignRight()
                                          .Background(Colors.Green.Medium)
                                          .PaddingVertical(4).PaddingHorizontal(10)
                                          .CornerRadius(6)
                                          .Text(t => t.Span("PAGO").SemiBold().FontColor(Colors.White));
                                    });

                                });
                            });
                        });
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(16);

                        // Bloco com detalhes (esquerda / direita)
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Column(left =>
                            {
                                left.Spacing(4);
                                left.Item().Text("Dados da Empresa").SemiBold();
                                left.Item().Text(company);
                                if (!string.IsNullOrWhiteSpace(settings.SupportEmail))
                                    left.Item().Text(settings.SupportEmail);
                            });

                            r.RelativeItem().Column(right =>
                            {
                                right.Spacing(4);
                                right.Item().Text("Detalhes da Fatura").SemiBold();
                                right.Item().Text($"Data do pagamento: {paidAtTx}");
                                right.Item().Text($"Método: {method}");
                                right.Item().Text($"Referência: {reference}");
                                right.Item().Text($"Competência: {compTx}");
                            });
                        });

                        // Tabela com item da quota
                        col.Item().Element(tableContainer =>
                        {
                            tableContainer.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(6);   // Descrição
                                    cols.RelativeColumn(2);   // Competência
                                    cols.RelativeColumn(2);   // Valor
                                });

                                // Cabeçalho
                                table.Header(h =>
                                {
                                    h.Cell().Element(HeaderCell).Text("Descrição");
                                    h.Cell().Element(HeaderCell).Text("Competência");
                                    h.Cell().Element(HeaderCell).AlignRight().Text("Valor");

                                    static IContainer HeaderCell(IContainer c) =>
                                        c.DefaultTextStyle(t => t.SemiBold())
                                         .Background(Colors.Grey.Lighten3)
                                         .PaddingVertical(6).PaddingHorizontal(8);
                                });

                                // Item único: Quota do mês
                                table.Cell().Element(BodyCell).Text($"Quota condominial");
                                table.Cell().Element(BodyCell).Text(compTx);
                                table.Cell().Element(BodyCell).AlignRight().Text(amountTx);

                                static IContainer BodyCell(IContainer c) =>
                                    c.PaddingVertical(6).PaddingHorizontal(8);
                            });
                        });

                        // Totais (simples, só um item)
                        col.Item().AlignRight().Column(tot =>
                        {
                            tot.Spacing(3);
                            tot.Item().Row(r =>
                            {
                                r.RelativeItem().AlignRight().Text("Subtotal:");
                                r.ConstantItem(120).AlignRight().Text(amountTx);
                            });
                            // Se quiseres mostrar multa/juros no futuro, somas aqui.
                            tot.Item().Row(r =>
                            {
                                r.RelativeItem().AlignRight().Text("Total pago:").SemiBold();
                                r.ConstantItem(120).AlignRight().Text(amountTx).SemiBold();
                            });
                        });

                        // Recibo do provedor (link clicável)
                        if (!string.IsNullOrWhiteSpace(provUrl))
                        {
                            col.Item().Element(e =>
                                e.Hyperlink(provUrl)
                                 .Text(t =>
                                 {
                                     t.Span("Recibo do provedor (Stripe): ").Light();
                                     t.Span(provUrl).Underline();
                                 })
                            );
                        }

                        // Observação
                        col.Item().BorderTop(1).PaddingTop(8)
                           .Text("Este documento é válido como comprovativo de pagamento.")
                           .Light();
                    });

                    page.Footer().AlignCenter().Text($"Gerado em {DateTime.Now:yyyy-MM-dd HH:mm}");
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", $"invoice_{payment.Id}.pdf");
        }



    }
}

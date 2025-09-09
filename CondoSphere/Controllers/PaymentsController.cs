using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CondoSphere.Data;
using CondoSphere.Models;
using CondoSphere.Data.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using CondoSphere.Services;
using CondoSphere.Messaging;
using System.Security.Claims;

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

        public PaymentsController(
            IPaymentRepository payments,
            IQuotaRepository quotas,
            IPaymentService paymentService,
            IConfiguration cfg,
            DomainNotificationService notify)
        {
            _payments = payments;
            _quotas = quotas;
            _paymentService = paymentService;
            _cfg = cfg;
            _notify = notify;
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


        // ------- Delete -------
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var payment = await _payments.GetByIdDetailedAsync(id);
            if (payment == null) return NotFound();
            return View(payment);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _payments.DeleteAsync(id);
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
    }
}

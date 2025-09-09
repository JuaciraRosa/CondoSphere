using CondoSphere.Data;
using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using CondoSphere.ModelTest;
using CondoSphere.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CondoSphere.Controllers
{
    [Authorize]
    public class QuotasController : Controller
    {
        private readonly IQuotaRepository _quotaRepository;
        private readonly IQuotaService _quotaService;
        private readonly ICondominiumRepository _condoRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IPaymentService _paymentService;

        public QuotasController(
            IQuotaRepository quotaRepository,
            IQuotaService quotaService,
            ICondominiumRepository condoRepository,
            IUnitRepository unitRepository,
            IPaymentService paymentService)
        {
            _quotaRepository = quotaRepository;
            _quotaService = quotaService;
            _condoRepository = condoRepository;
            _unitRepository = unitRepository;
            _paymentService = paymentService;
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var list = await _quotaRepository.GetAllWithUnitAsync();
            // ordene como preferir
            return View(list.OrderByDescending(q => q.DueDate).ToList());
        }

        // GET /Quotas/Pay/5
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Pay(int id)
        {
            var quota = await _quotaRepository.GetByIdAsync(id);
            if (quota == null) return NotFound();
            if (quota.IsPaid)
            {
                TempData["ok"] = "Quota já está paga.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new QuotaPayVM
            {
                QuotaId = quota.Id,
                Amount = quota.Amount,
                Description = $"Quota #{quota.Id} — {quota.DueDate:yyyy-MM}"
            };
            return View(vm);
        }

        // POST /Quotas/StartCheckout
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartCheckout(int id)
        {
            try
            {
                var success = Url.Action("StripeSuccess", "Quotas", new { id }, Request.Scheme)!;
                var cancel = Url.Action("StripeCancel", "Quotas", new { id }, Request.Scheme)!;

                var url = await _paymentService.CreateCheckoutSessionForQuotaAsync(id, success, cancel);
                return Redirect(url);
            }
            catch (Exception ex)
            {
                TempData["pay_error"] = ex.Message;
                return RedirectToAction(nameof(Pay), new { id });
            }
        }

        // GET /Quotas/StripeSuccess?id=5&session_id=cs_test_...
        [Authorize]
        [HttpGet]
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> StripeSuccess(int id, [FromQuery(Name = "session_id")] string sessionId)
        {
            // guard extra: evita placeholder
            if (string.IsNullOrWhiteSpace(sessionId) || sessionId.Contains("{CHECKOUT_SESSION_ID}"))
            {
                TempData["pay_error"] = "Sessão inválida.";
                return RedirectToAction(nameof(Pay), new { id });
            }

            try
            {
                var sessionSrv = new SessionService();
                var ss = await sessionSrv.GetAsync(sessionId);

                if (ss.PaymentStatus == "paid" && !string.IsNullOrEmpty(ss.PaymentIntentId))
                {
                    await _paymentService.ConfirmAndMarkAsync(ss.PaymentIntentId);
                    TempData["ok"] = "Pagamento concluído.";
                    return RedirectToAction(nameof(Index));
                }

                TempData["pay_error"] = "Sessão não paga.";
                return RedirectToAction(nameof(Pay), new { id });
            }
            catch (Stripe.StripeException sx)
            {
                TempData["pay_error"] = sx.Message;
                return RedirectToAction(nameof(Pay), new { id });
            }
            catch (Exception ex)
            {
                TempData["pay_error"] = ex.Message;
                return RedirectToAction(nameof(Pay), new { id });
            }
        }

        // GET /Quotas/StripeCancel?id=5
        [Authorize]
        [HttpGet]
        public IActionResult StripeCancel(int id)
        {
            TempData["pay_error"] = "Pagamento cancelado.";
            return RedirectToAction(nameof(Pay), new { id });
        }
    }
}

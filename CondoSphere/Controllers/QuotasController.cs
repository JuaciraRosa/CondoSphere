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
            await PopulateCondominiumsAsync(); 
            var list = await _quotaRepository.GetAllWithUnitAsync();
            return View(list.OrderByDescending(q => q.DueDate).ToList());
        }


        [Authorize(Roles = "Administrator,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateMonthly(int condominiumId, int year, int month, decimal amount)
        {
            // validações simples
            if (condominiumId <= 0)
                ModelState.AddModelError(nameof(condominiumId), "Selecione um condomínio.");
            if (year < 2000 || year > 2100)
                ModelState.AddModelError(nameof(year), "Ano inválido.");
            if (month < 1 || month > 12)
                ModelState.AddModelError(nameof(month), "Mês inválido (1..12).");
            if (amount <= 0)
                ModelState.AddModelError(nameof(amount), "Valor deve ser maior que zero.");

            if (!ModelState.IsValid)
            {
                // volta ao Index com os erros e o dropdown preenchido
                await PopulateCondominiumsAsync(condominiumId);
                var list = await _quotaRepository.GetAllWithUnitAsync();
                return View(nameof(Index), list.OrderByDescending(q => q.DueDate).ToList());
            }

            // chama o serviço que cria as quotas do mês
            var created = await _quotaService.EnsureMonthlyAsync(condominiumId, year, month, amount);

            TempData["Success"] = created == 0
                ? $"Nenhuma quota nova para {year}-{month:D2} (já existiam)."
                : $"{created} quota(s) gerada(s) para {year}-{month:D2}.";

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrator,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateRangeMonthly(
    int condominiumId, int fromYear, int fromMonth, int toYear, int toMonth, decimal amount)
        {
            if (condominiumId <= 0)
                ModelState.AddModelError(nameof(condominiumId), "Selecione um condomínio.");
            if (fromMonth < 1 || fromMonth > 12 || toMonth < 1 || toMonth > 12)
                ModelState.AddModelError("", "Meses devem estar entre 1 e 12.");
            if (fromYear < 2000 || toYear > 2100 || fromYear > toYear ||
                (fromYear == toYear && fromMonth > toMonth))
                ModelState.AddModelError("", "Intervalo de datas inválido.");
            if (amount <= 0)
                ModelState.AddModelError(nameof(amount), "Valor deve ser maior que zero.");

            if (!ModelState.IsValid)
            {
                await PopulateCondominiumsAsync(condominiumId);
                var list = await _quotaRepository.GetAllWithUnitAsync();
                return View(nameof(Index), list.OrderByDescending(q => q.DueDate).ToList());
            }

            var from = new DateTime(fromYear, fromMonth, 1);
            var to = new DateTime(toYear, toMonth, 1);
            var created = await _quotaService.EnsureRangeMonthlyAsync(condominiumId, from, to, amount);

            TempData["Success"] = created == 0
                ? $"Nenhuma quota nova no intervalo {from:yyyy-MM}..{to:yyyy-MM}."
                : $"{created} quota(s) gerada(s) no intervalo {from:yyyy-MM}..{to:yyyy-MM}.";

            return RedirectToAction(nameof(Index));
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
                TempData["Success"] = "Quota já está paga.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new QuotaPayVM
            {
                QuotaId = quota.Id,
                Amount = quota.Amount,
                Description = $"Quota {quota.Id} — {quota.DueDate:yyyy-MM}"
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
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Pay), new { id });
            }
        }

        // GET /Quotas/StripeSuccess?id=5&session_id=cs_test_...
        [Authorize]
        [HttpGet]
     
        public async Task<IActionResult> StripeSuccess(int id, [FromQuery(Name = "session_id")] string sessionId)
        {
            // guard extra: evita placeholder
            if (string.IsNullOrWhiteSpace(sessionId) || sessionId.Contains("{CHECKOUT_SESSION_ID}"))
            {
                TempData["Error"] = "Sessão inválida.";
                return RedirectToAction(nameof(Pay), new { id });
            }

            try
            {
                var sessionSrv = new SessionService();
                var ss = await sessionSrv.GetAsync(sessionId);

                if (ss.PaymentStatus == "paid" && !string.IsNullOrEmpty(ss.PaymentIntentId))
                {
                    await _paymentService.ConfirmAndMarkAsync(ss.PaymentIntentId);
                    TempData["Success"] = "Pagamento concluído.";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Error"] = "Sessão não paga.";
                return RedirectToAction(nameof(Pay), new { id });
            }
            catch (Stripe.StripeException sx)
            {
                TempData["Error"] = sx.Message;
                return RedirectToAction(nameof(Pay), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Pay), new { id });
            }
        }

        // GET /Quotas/StripeCancel?id=5
        [Authorize]
        [HttpGet]
        public IActionResult StripeCancel(int id)
        {
            TempData["Error"] = "Pagamento cancelado.";
            return RedirectToAction(nameof(Pay), new { id });
        }

        // ------- helpers para views -------
        private async Task PopulateUnitSelectAsync(int? selectedUnitId = null)
        {
            var units = await _unitRepository.GetAllAsync();
            // Ajuste o "Text" se sua classe Unit tiver Number/Name/etc.
            var items = units.Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = $"Unidade {u.Id}"
            }).ToList();

            ViewBag.UnitId = new SelectList(items, "Value", "Text", selectedUnitId);
        }

        // ------- Details -------
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var quota = await _quotaRepository.GetByIdAsync(id);
            if (quota == null) return NotFound();
            return View(quota);
        }

        // ------- Edit (GET) -------
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var quota = await _quotaRepository.GetByIdAsync(id);
            if (quota == null) return NotFound();

            await PopulateUnitSelectAsync(quota.UnitId);
            return View(quota);
        }

        // ------- Edit (POST) -------
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Quota model)
        {
            if (id != model.Id) return BadRequest();

            // Evita validação de navegações
            ModelState.Remove("Unit");
            ModelState.Remove("Payment");

            if (!ModelState.IsValid)
            {
                await PopulateUnitSelectAsync(model.UnitId);
                return View(model);
            }

            await _quotaRepository.UpdateAsync(model);
            TempData["Success"] = "Quota atualizada com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Quotas/Delete/{id}
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var quota = await _quotaRepository.GetByIdAsync(id);
            if (quota == null) return NotFound();
            return View(quota);
        }

        // POST: Quotas/Delete/{id}
        [Authorize]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var q = await _quotaRepository.GetByIdAsync(id);
                if (q == null)
                {
                    TempData["Error"] = "Quota not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (q.IsPaid || q.Payment != null)
                {
                    TempData["Error"] = "You cannot delete a paid quota. Refund or cancel the payment first.";
                    return RedirectToAction(nameof(Index));
                }

                await _quotaRepository.DeleteAsync(id);
                TempData["Success"] = "Quota deleted successfully.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Quota could not be deleted due to related records.";
            }
            catch
            {
                TempData["Error"] = "An unexpected error occurred while deleting the quota.";
            }
            return RedirectToAction(nameof(Index));
        }


        // preenche o <select name="condominiumId" ... asp-items="ViewBag.Condominiums">
        private async Task PopulateCondominiumsAsync(int? selectedId = null)
        {
            var condos = await _condoRepository.GetAllAsync();
            var items = condos.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.Name} "
            }).ToList();

            ViewBag.Condominiums = new SelectList(items, "Value", "Text", selectedId);
        }


    }
}

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

namespace CondoSphere.Controllers
{
    [Authorize(Roles = "Administrator,Manager,Resident")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

    public class PaymentsController : Controller
    {
        private readonly IPaymentRepository _payments;
        private readonly IQuotaRepository _quotas;

        public PaymentsController(IPaymentRepository payments, IQuotaRepository quotas)
        {
            _payments = payments;
            _quotas = quotas;
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

        // ------- Create -------
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadSelects();
            return View(new Payment { Status = PaymentStatusType.Pending });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Payment model)
        {
            if (!ModelState.IsValid)
            {
                await LoadSelects(model);
                return View(model);
            }

            // campos de sistema
            model.CreatedAt = DateTime.UtcNow;

            await _payments.AddAsync(model);
            return RedirectToAction(nameof(Index));
        }

        // ------- Edit -------
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var payment = await _payments.GetByIdDetailedAsync(id);
            if (payment == null) return NotFound();

            await LoadSelects(payment);
            return View(payment);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Payment model)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadSelects(model);
                return View(model);
            }

            _payments.Update(model);
            await _payments.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
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
    }
}

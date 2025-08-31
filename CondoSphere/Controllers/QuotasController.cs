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
using Microsoft.AspNetCore.Authorization;
using CondoSphere.Services;
using CondoSphere.ModelTest;

namespace CondoSphere.Controllers
{
    [Authorize(Roles = "Administrator,Manager")]
    public class QuotasController : Controller
    {
        private readonly IQuotaRepository _quotaRepository;
        private readonly IQuotaService _quotaService;

        // 🔹 ADICIONE essas dependências
        private readonly ICondominiumRepository _condoRepository;
        private readonly IUnitRepository _unitRepository;

        public QuotasController(IQuotaRepository quotaRepository, IQuotaService quotaService, ICondominiumRepository condoRepository,   
        IUnitRepository unitRepository)
        {
            _quotaRepository = quotaRepository;
            _quotaService = quotaService;
            _condoRepository = condoRepository;
            _unitRepository = unitRepository;
        }

        public async Task<IActionResult> Index()
        {
            // 🔹 popular dropdown de condomínios para o formulário "Generate quotas"
            var condos = await _condoRepository.GetAllWithCompanyAsync();
            ViewBag.Condominiums = new SelectList(
                condos.Select(c => new { c.Id, Name = $"{c.Name} ({c.Company?.Name})" }),
                "Id", "Name"
            );

            var quotas = await _quotaRepository.GetAllWithUnitAsync();
            return View(quotas);
        }
        public async Task<IActionResult> Details(int id)
        {
            var quota = await _quotaRepository.GetByIdWithUnitAsync(id);
            if (quota == null) return NotFound();
            return View(quota);
        }


        public async Task<IActionResult> Create()
        {
            await LoadUnitsSelectAsync(); // 🔹 carrega ViewBag.UnitId
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Quota quota)
        {
            if (!ModelState.IsValid)
            {
                await LoadUnitsSelectAsync(quota.UnitId);
                return View(quota);
            }

            await _quotaRepository.AddAsync(quota);
            return RedirectToAction(nameof(Index));
        }

        // ----------------- EDIT -----------------

        public async Task<IActionResult> Edit(int id)
        {
            var quota = await _quotaRepository.GetByIdWithUnitAsync(id);
            if (quota == null) return NotFound();

            await LoadUnitsSelectAsync(quota.UnitId);
            return View(quota);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Quota quota)
        {
            if (id != quota.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadUnitsSelectAsync(quota.UnitId);
                return View(quota);
            }

            _quotaRepository.Update(quota);
            await _quotaRepository.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> Delete(int id)
        {
            var quota = await _quotaRepository.GetByIdWithUnitAsync(id);
            if (quota == null) return NotFound();
            return View(quota);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _quotaRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // ----------------- GENERATE -----------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateMonthly(int condominiumId, int year, int month, decimal amount)
        {
            try
            {
                var created = await _quotaService.EnsureMonthlyAsync(condominiumId, year, month, amount);
                TempData["Ok"] = $"{created} quota(s) generated for {month:D2}/{year}.";
            }
            catch (Exception ex)
            {
                TempData["Err"] = $"Generation failed: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        // ----------------- PAY (mantém) -----------------

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Pay(int id)
        {
            var quota = await _quotaRepository.GetByIdAsync(id);
            if (quota == null) return NotFound();
            if (quota.IsPaid) return BadRequest("Quota already paid.");

            var vm = new QuotaPayVM
            {
                QuotaId = quota.Id,
                Amount = quota.Amount,
                Description = $"Quota #{quota.Id} — {quota.DueDate:yyyy-MM}"
            };
            return View(vm);
        }

        // ----------------- HELPERS -----------------

        // Carrega TODAS as unidades. Se você preferir, crie um Create com CondoId primeiro e filtre por condomínio.
        private async Task LoadUnitsSelectAsync(int? selectedUnitId = null)
        {
            var units = await _unitRepository.GetAllAsync();
            ViewBag.UnitId = new SelectList(units, "Id", "Number", selectedUnitId);
        }



    }
}

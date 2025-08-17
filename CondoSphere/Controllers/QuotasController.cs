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

        public QuotasController(IQuotaRepository quotaRepository, IQuotaService quotaService)
        {
            _quotaRepository = quotaRepository;
            _quotaService = quotaService;
        }

        public async Task<IActionResult> Index()
        {
            var quotas = await _quotaRepository.GetAllAsync();
            return View(quotas);
        }



        public async Task<IActionResult> Details(int id)
        {
            var quota = await _quotaRepository.GetByIdAsync(id);
            if (quota == null) return NotFound();
            return View(quota);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Quota quota)
        {
            if (!ModelState.IsValid) return View(quota);
            await _quotaRepository.AddAsync(quota);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var quota = await _quotaRepository.GetByIdAsync(id);
            if (quota == null) return NotFound();
            return View(quota);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Quota quota)
        {
            if (id != quota.Id) return NotFound();
            if (!ModelState.IsValid) return View(quota);

            _quotaRepository.Update(quota);
            await _quotaRepository.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var quota = await _quotaRepository.GetByIdAsync(id);
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

        // ===== NEW: generation actions =====

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateRange(int condominiumId, DateTime from, DateTime to, decimal amount)
        {
            try
            {
                var created = await _quotaService.EnsureRangeMonthlyAsync(condominiumId, from, to, amount);
                TempData["Ok"] = $"{created} quota(s) generated for {from:yyyy-MM}..{to:yyyy-MM}.";
            }
            catch (Exception ex)
            {
                TempData["Err"] = $"Generation failed: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// test endpoint
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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




    }
}

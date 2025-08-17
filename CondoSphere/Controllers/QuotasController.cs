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

namespace CondoSphere.Controllers
{
    public class QuotasController : Controller
    {
        private readonly IQuotaRepository _quotaRepository;

        public QuotasController(IQuotaRepository quotaRepository)
        {
            _quotaRepository = quotaRepository;
        }

        public async Task<IActionResult> Index()
        {
            var quotas = await _quotaRepository.GetAllAsync();
            return View(quotas);
        }

        // GET: Quotas/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var quota = await _quotaRepository.GetByIdAsync(id);
            if (quota == null)
                return NotFound();

            return View(quota);
        }


        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Quota quota)
        {
            if (ModelState.IsValid)
            {
                await _quotaRepository.AddAsync(quota);
                return RedirectToAction(nameof(Index));
            }
            return View(quota);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var quota = await _quotaRepository.GetByIdAsync(id);
            if (quota == null) return NotFound();
            return View(quota);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Quota quota)
        {
            if (id != quota.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _quotaRepository.Update(quota);
                await _quotaRepository.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(quota);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var quota = await _quotaRepository.GetByIdAsync(id);
            if (quota == null) return NotFound();
            return View(quota);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _quotaRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}

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

namespace CondoSphere.Controllers
{
    [Authorize(Roles = "Administrator,Manager")]
    public class CondominiumsController : Controller
    {
        private readonly ICondominiumRepository _condoRepo;
        private readonly ICompanyRepository _companyRepo;

        public CondominiumsController(ICondominiumRepository condoRepo, ICompanyRepository companyRepo)
        {
            _condoRepo = condoRepo;
            _companyRepo = companyRepo;
        }

        public async Task<IActionResult> Index()
        {
            var condos = await _condoRepo.GetAllWithCompanyAsync();
            return View(condos);
        }

        public async Task<IActionResult> Details(int id)
        {
            var condo = await _condoRepo.GetDetailsAsync(id);  // inclui Units/Expenses/Requests
            if (condo == null) return NotFound();
            return View(condo);
        }


     
        public async Task<IActionResult> Create()
        {
            await LoadCompaniesSelectAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Condominium condominium)
        {
            if (ModelState.IsValid)
            {
                await _condoRepo.AddAsync(condominium);
                return RedirectToAction(nameof(Index));
            }
            await LoadCompaniesSelectAsync(condominium.CompanyId);
            return View(condominium);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var condo = await _condoRepo.GetByIdAsync(id);
            if (condo == null) return NotFound();

            await LoadCompaniesSelectAsync(condo.CompanyId);
            return View(condo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Condominium condominium)
        {
            if (id != condominium.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _condoRepo.Update(condominium);
                await _condoRepo.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            await LoadCompaniesSelectAsync(condominium.CompanyId);
            return View(condominium);
        }

        // GET: Condominiums/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var condo = await _condoRepo.GetByIdAsync(id);
            if (condo == null) return NotFound();
            return View(condo);
        }

        // POST: Condominiums/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _condoRepo.DeleteAsync(id);
                TempData["Success"] = "Condominium deleted successfully.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Condominium cannot be deleted because it has related data (e.g., units, quotas, payments).";
            }
            catch
            {
                TempData["Error"] = "An unexpected error occurred while deleting the condominium.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadCompaniesSelectAsync(int? selectedId = null)
        {
            var companies = await _companyRepo.GetAllAsync();
            ViewBag.CompanyId = new SelectList(companies, "Id", "Name", selectedId);
        }
    }
}

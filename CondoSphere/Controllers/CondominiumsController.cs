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
    public class CondominiumsController : Controller
    {
        private readonly ICondominiumRepository _condoRepo;

        public CondominiumsController(ICondominiumRepository condoRepo)
        {
            _condoRepo = condoRepo;
        }

        public async Task<IActionResult> Index()
        {
            var condos = await _condoRepo.GetAllAsync();
            return View(condos);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Condominium condominium)
        {
            if (ModelState.IsValid)
            {
                await _condoRepo.AddAsync(condominium);
                return RedirectToAction(nameof(Index));
            }
            return View(condominium);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var condo = await _condoRepo.GetByIdAsync(id);
            if (condo == null) return NotFound();
            return View(condo);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Condominium condominium)
        {
            if (id != condominium.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _condoRepo.Update(condominium);
                await _condoRepo.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(condominium);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var condo = await _condoRepo.GetByIdAsync(id);
            if (condo == null) return NotFound();
            return View(condo);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _condoRepo.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}

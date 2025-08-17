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
    public class UnitsController : Controller
    {
        private readonly IUnitRepository _unitRepo;

        public UnitsController(IUnitRepository unitRepo)
        {
            _unitRepo = unitRepo;
        }

        public async Task<IActionResult> Index()
        {
            var units = await _unitRepo.GetAllAsync();
            return View(units);
        }

        // GET: Units/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var unit = await _unitRepo.GetByIdAsync(id);
            if (unit == null)
                return NotFound();

            return View(unit);
        }


        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Unit unit)
        {
            if (ModelState.IsValid)
            {
                await _unitRepo.AddAsync(unit);
                return RedirectToAction(nameof(Index));
            }
            return View(unit);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var unit = await _unitRepo.GetByIdAsync(id);
            if (unit == null) return NotFound();
            return View(unit);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Unit unit)
        {
            if (id != unit.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _unitRepo.Update(unit);
                await _unitRepo.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(unit);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var unit = await _unitRepo.GetByIdAsync(id);
            if (unit == null) return NotFound();
            return View(unit);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _unitRepo.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}

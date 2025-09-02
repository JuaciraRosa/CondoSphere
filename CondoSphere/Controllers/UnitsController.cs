using CondoSphere.Data;
using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CondoSphere.Controllers
{
    [Authorize]
    public class UnitsController : Controller
    {
        private readonly IUnitRepository _units;
        private readonly ICondominiumRepository _condos;
        private readonly IUserRepository _users;

        public UnitsController(
            IUnitRepository units,
            ICondominiumRepository condos,
            IUserRepository users)
        {
            _units = units;
            _condos = condos;
            _users = users;
        }

        public async Task<IActionResult> Index()
        {
            var items = await _units.GetAllDetailedAsync(); // inclui Condominium + Owner
            return View(items);
        }

        public async Task<IActionResult> Details(int id)
        {
            var unit = await _units.GetByIdDetailedAsync(id);
            if (unit is null) return NotFound();
            return View(unit);
        }

        // GET: Units/Create
        public async Task<IActionResult> Create()
        {
            await PopulateSelectsAsync();
            return View(new Unit());
        }

        // POST: Units/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Unit unit)
        {
            if (!ModelState.IsValid)
            {
                await PopulateSelectsAsync(unit.CondominiumId, unit.OwnerId);
                return View(unit);
            }

            await _units.AddAsync(unit);
            await _units.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Units/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var unit = await _units.GetByIdAsync(id); // para editar basta o “simples”
            if (unit is null) return NotFound();

            await PopulateSelectsAsync(unit.CondominiumId, unit.OwnerId);
            return View(unit);
        }

        // POST: Units/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Unit unit)
        {
            if (id != unit.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateSelectsAsync(unit.CondominiumId, unit.OwnerId);
                return View(unit);
            }

            _units.Update(unit);
            await _units.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Units/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var unit = await _units.GetByIdDetailedAsync(id); // com nomes para mostrar
            if (unit is null) return NotFound();
            return View(unit);
        }

        // POST: Units/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _units.DeleteAsync(id);
            await _units.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // -------- helpers --------
        private static string OwnerDisplay(User u)
            => string.IsNullOrWhiteSpace(u.FullName) ? u.Email : u.FullName;

        private async Task PopulateSelectsAsync(int? condominiumId = null, string? ownerId = null)
        {
            var condos = await _condos.GetAllAsync();
            ViewBag.CondominiumId = new SelectList(condos.OrderBy(c => c.Name), "Id", "Name", condominiumId);

            var users = await _users.GetAllAsync();
            var ownerItems = users
                .Select(u => new { u.Id, Name = OwnerDisplay(u) })
                .OrderBy(x => x.Name)
                .ToList();

            ViewBag.OwnerId = new SelectList(ownerItems, "Id", "Name", ownerId);
        }
    }


}

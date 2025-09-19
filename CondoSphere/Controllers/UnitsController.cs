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
using System.Security.Claims;
using System.Threading.Tasks;

namespace CondoSphere.Controllers
{
    [Authorize]
    public class UnitsController : Controller
    {
        private readonly IUnitRepository _units;
        private readonly ICondominiumRepository _condos;
        private readonly IUserRepository _users;
        private readonly IUnitOwnershipRepository _ownerships;

        public UnitsController(
            IUnitRepository units,
            ICondominiumRepository condos,
            IUserRepository users, IUnitOwnershipRepository ownerships)
        {
            _units = units;
            _condos = condos;
            _users = users;
            _ownerships = ownerships;
        }


        [HttpGet]
        public async Task<IActionResult> Index(int? condominiumId, string? number, int page = 1, int pageSize = 20)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Garante lista materializada e nunca nula
            var items = (await _units.GetAllDetailedAsync())?.ToList() ?? new List<Unit>();

            // Resident vê só a própria unidade
            if (string.Equals(role, "Resident", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(userId))
                items = items.Where(u => u.OwnerId == userId).ToList();

            // Filtros
            if (condominiumId.HasValue)
                items = items.Where(u => u.CondominiumId == condominiumId.Value).ToList();

            if (!string.IsNullOrWhiteSpace(number))
                items = items
                    .Where(u => (u.Number ?? string.Empty)
                    .Contains(number.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ToList();

            // Ordena (evita null no nome do condomínio)
            items = items
                .OrderBy(u => u.Condominium?.Name ?? string.Empty)
                .ThenBy(u => u.Number ?? string.Empty)
                .ToList();

            // Paginação simples (com clamps)
            pageSize = Math.Max(1, pageSize);
            var total = items.Count;
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            page = Math.Min(Math.Max(1, page), totalPages);

            var pageItems = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // Dropdown de condomínios para filtro
            var condos = await _condos.GetAllAsync();
            ViewBag.FilterCondominiums = new SelectList(condos.OrderBy(c => c.Name), "Id", "Name", condominiumId);
            ViewBag.Number = number;
            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.CondominiumId = condominiumId;

            return View(pageItems);
        }


        // DETAILS: residente só pode ver a própria unidade
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var unit = await _units.GetByIdDetailedAsync(id);
            if (unit is null) return NotFound();

            if (User.IsInRole("Resident"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.Equals(unit.OwnerId, userId, StringComparison.Ordinal))
                    return Forbid();
            }
            return View(unit);
        }


        // GET: Units/Create
        [Authorize(Roles = "Administrator,Manager")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateSelectsAsync();
            return View(new Unit());
        }

        [Authorize(Roles = "Administrator,Manager")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Unit unit)
        {
            if (!ModelState.IsValid)
            {
                await PopulateSelectsAsync(unit.CondominiumId, unit.OwnerId);
                return View(unit);
            }

            // regra: número único por condomínio
            if (await _units.NumberExistsInCondoAsync(unit.CondominiumId, unit.Number))
            {
                ModelState.AddModelError(nameof(Unit.Number), "This unit number already exists in the selected condominium.");
                await PopulateSelectsAsync(unit.CondominiumId, unit.OwnerId);
                return View(unit);
            }

            await _units.AddAsync(unit);
            await _units.SaveChangesAsync();


            if (!string.IsNullOrWhiteSpace(unit.OwnerId))
            {
                await _ownerships.CloseOpenAsync(unit.Id);
                await _ownerships.AddStartAsync(unit.Id, unit.OwnerId);
                await _ownerships.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }


        [Authorize(Roles = "Administrator,Manager")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var unit = await _units.GetByIdAsync(id);
            if (unit is null) return NotFound();

            await PopulateSelectsAsync(unit.CondominiumId, unit.OwnerId);
            return View(unit);
        }

        [Authorize(Roles = "Administrator,Manager")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Unit unit)
        {
            if (id != unit.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateSelectsAsync(unit.CondominiumId, unit.OwnerId);
                return View(unit);
            }

            // validar unicidade do número
            if (await _units.NumberExistsInCondoAsync(unit.CondominiumId, unit.Number, exceptId: unit.Id))
            {
                ModelState.AddModelError(nameof(Unit.Number), "This unit number already exists in the selected condominium.");
                await PopulateSelectsAsync(unit.CondominiumId, unit.OwnerId);
                return View(unit);
            }

            // Carrega a entidade rastreada
            var db = await _units.GetByIdAsync(id);
            if (db is null) return NotFound();

            // Guarda o dono anterior para histórico
            var oldOwnerId = db.OwnerId;

            // Copia os campos permitidos
            db.Number = unit.Number?.Trim();
            db.Area = unit.Area;
            db.CondominiumId = unit.CondominiumId;
            db.OwnerId = unit.OwnerId;
            db.IsActive = unit.IsActive; // se editar este campo no formulário

            // Salva (não chame Update aqui)
            await _units.SaveChangesAsync();

            // Histórico: se o dono mudou, fecha e abre registro
            if (!string.Equals(oldOwnerId, db.OwnerId, StringComparison.Ordinal))
            {
                await _ownerships.CloseOpenAsync(db.Id);

                if (!string.IsNullOrWhiteSpace(db.OwnerId))
                    await _ownerships.AddStartAsync(db.Id, db.OwnerId);

                await _ownerships.SaveChangesAsync();
            }

            TempData["Success"] = "Unit updated successfully.";
            return RedirectToAction(nameof(Index));
        }



        // POST: Units/Delete/5  (soft delete)
        [Authorize(Roles = "Administrator,Manager")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var entity = await _units.GetByIdAsync(id);
                if (entity == null)
                {
                    TempData["Error"] = "Unit not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (!entity.IsActive)
                {
                    TempData["Success"] = "Unit is already inactive.";
                    return RedirectToAction(nameof(Index));
                }

                entity.IsActive = false;
                _units.Update(entity);
                await _units.SaveChangesAsync();

                TempData["Success"] = "Unit deactivated successfully.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Unit could not be deactivated due to related records.";
            }
            catch
            {
                TempData["Error"] = "An unexpected error occurred while deactivating the unit.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Units/Activate/5
        [Authorize(Roles = "Administrator,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            try
            {
                var entity = await _units.GetByIdAsync(id);
                if (entity == null)
                {
                    TempData["Error"] = "Unit not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (entity.IsActive)
                {
                    TempData["Success"] = "Unit is already active.";
                    return RedirectToAction(nameof(Index));
                }

                entity.IsActive = true;
                _units.Update(entity);
                await _units.SaveChangesAsync();

                TempData["Success"] = "Unit activated successfully.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Unit could not be activated due to related records.";
            }
            catch
            {
                TempData["Error"] = "An unexpected error occurred while activating the unit.";
            }

            return RedirectToAction(nameof(Index));
        }



        // -------- helpers --------
        private static string OwnerDisplay(User u)
            => string.IsNullOrWhiteSpace(u.FullName) ? u.Email : u.FullName;


        // --- AJAX: load owners by Condominium (active + same company, no role filter) ---
        [HttpGet]
        public async Task<IActionResult> OwnersByCondominium(int condominiumId)
        {
            var condo = await _condos.GetByIdAsync(condominiumId);
            if (condo == null) return Json(Array.Empty<object>());

            var users = await _users.GetAllAsync();

            var items = users
                .Where(u => u.IsActive && u.CompanyId == condo.CompanyId)
                .Select(u => new
                {
                    value = u.Id,
                    text = string.IsNullOrWhiteSpace(u.FullName) ? (u.Email ?? "(no email)") : u.FullName
                })
                .OrderBy(x => x.text)
                .ToList();

            return Json(items);
        }

        // --- Populate dropdowns (active users; if a condo is chosen, constrain by the same company) ---
        private async Task PopulateSelectsAsync(int? condominiumId = null, string? ownerId = null)
        {
            // Condominiums
            var condos = await _condos.GetAllAsync();
            ViewBag.CondominiumId = new SelectList(condos.OrderBy(c => c.Name), "Id", "Name", condominiumId);

            // Users (potential owners)
            var users = await _users.GetAllAsync();

            int? companyFilter = null;
            if (condominiumId.HasValue)
            {
                var condo = condos.FirstOrDefault(c => c.Id == condominiumId.Value)
                            ?? await _condos.GetByIdAsync(condominiumId.Value);
                companyFilter = condo?.CompanyId;
            }

            var owners = users
                .Where(u => u.IsActive && (!companyFilter.HasValue || u.CompanyId == companyFilter))
                .Select(u => new
                {
                    u.Id,
                    Name = string.IsNullOrWhiteSpace(u.FullName) ? (u.Email ?? "(no email)") : u.FullName
                })
                .OrderBy(x => x.Name)
                .ToList();

            ViewBag.OwnerId = new SelectList(owners, "Id", "Name", ownerId);
        }


    }


}

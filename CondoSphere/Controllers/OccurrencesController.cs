using CondoSphere.Data.Interfaces;
using CondoSphere.Features.Ocurrences;
using CondoSphere.Messaging;
using CondoSphere.Services.AppData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace CondoSphere.Controllers
{
    [Authorize]
    public class OccurrencesController : Controller
    {
        private readonly JsonFileStore<OccurrenceDto> _store;
        private readonly DomainNotificationService _notify;
        private readonly ICondominiumRepository _condos;
        private readonly IUnitRepository _units;
        public OccurrencesController(IWebHostEnvironment env,
                                     DomainNotificationService notify,
                                      ICondominiumRepository condos,
                                        IUnitRepository units)
        {
            _store = new JsonFileStore<OccurrenceDto>(env, "appdata/occurrences.json");
            _notify = notify;
            _condos = condos;
            _units = units;
        }

        public async Task<IActionResult> Index(int? condominiumId)
        {
            var list = await _store.ReadAllAsync();
            if (condominiumId.HasValue)
                list = list.Where(x => x.CondominiumId == condominiumId.Value).ToList();

            // Resident vê apenas as próprias ocorrências
            if (User.IsInRole("Resident"))
            {
                var email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
                if (!string.IsNullOrWhiteSpace(email))
                    list = list.Where(x => string.Equals(x.CreatedBy, email, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Mapa para mostrar nomes em vez do Id (Company)                       
            var lookup = (await _condos.GetAllWithCompanyAsync())
                .ToDictionary(c => c.Id, c => (Condo: c.Name, Company: c.Company?.Name));
            ViewBag.CondoLookup = lookup;

            return View(list.OrderByDescending(x => x.CreatedAt).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? condominiumId)
        {
            ViewBag.CondoId = condominiumId;
            await LoadCondominiumsSelectAsync(condominiumId);              
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OccurrenceDto model)
        {
            if (!ModelState.IsValid)                                      
            {
                await LoadCondominiumsSelectAsync(model.CondominiumId);
                return View(model);
            }

            // garante o autor
            if (string.IsNullOrWhiteSpace(model.CreatedBy))
                model.CreatedBy = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;

            var list = await _store.ReadAllAsync();
            model.CreatedAt = DateTime.UtcNow;
            list.Add(model);
            await _store.WriteAllAsync(list);

            // e-mail de confirmação
            if (!string.IsNullOrWhiteSpace(model.CreatedBy))
                await _notify.OccurrenceStatusChangedAsync(model.CreatedBy, model.Title, "Open");

            TempData["Success"] = "Ocorrência registada.";
            return RedirectToAction(nameof(Index), new { condominiumId = model.CondominiumId });
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> ChangeStatus(string id, string status)
        {
            var list = await _store.ReadAllAsync();
            var oc = list.FirstOrDefault(x => x.Id == id);
            if (oc == null) return NotFound();

            oc.Status = status;
            if (status == "Resolved") oc.ClosedAt = DateTime.UtcNow;
            await _store.WriteAllAsync(list);

            // notificar autor sobre a alteração de estado
            if (!string.IsNullOrWhiteSpace(oc.CreatedBy))
                await _notify.OccurrenceStatusChangedAsync(oc.CreatedBy, oc.Title, oc.Status);

            TempData["Success"] = "Estado atualizado e e-mail enviado.";
            return RedirectToAction(nameof(Index), new { condominiumId = oc.CondominiumId });
        }


        // ---------------- helpers ----------------                                  <-- NOVO
        private async Task LoadCondominiumsSelectAsync(int? selectedId = null)
        {
            var items = (await _condos.GetAllWithCompanyAsync())
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Company != null ? $"{c.Name} — {c.Company.Name}" : c.Name
                })
                .ToList();

            ViewBag.CondominiumId = new SelectList(items, "Value", "Text", selectedId?.ToString());
        }

        private async Task LoadUnitsSelectAsync(int? condominiumId)
        {
            var list = new List<SelectListItem>();

            if (condominiumId.HasValue && condominiumId.Value > 0)
            {
                var numbers = await _units.GetNumbersByCondominiumIdAsync(condominiumId.Value);
                list = numbers.Select(n => new SelectListItem { Value = n, Text = n }).ToList();
            }

            ViewBag.UnitNumbers = new SelectList(list, "Value", "Text");
        }


        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var list = await _store.ReadAllAsync();
            var oc = list.FirstOrDefault(x => x.Id == id);
            if (oc == null) return NotFound();

            if (User.IsInRole("Resident"))
            {
                var email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
                if (!string.IsNullOrWhiteSpace(email) &&
                    !string.Equals(oc.CreatedBy, email, StringComparison.OrdinalIgnoreCase))
                    return Forbid();
            }

            return View(oc);
        }


        [HttpGet]
        public async Task<IActionResult> UnitsByCondo(int condominiumId)
        {
            var numbers = await _units.GetNumbersByCondominiumIdAsync(condominiumId);
            var items = numbers.Select(n => new { value = n, text = n }).ToList();
            return Json(items);
        }


        // POST: Occurrences/Delete
        [HttpPost]
        [Authorize(Roles = "Administrator,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id, int? condominiumId)
        {
            try
            {
                var list = await _store.ReadAllAsync();
                var occ = list.FirstOrDefault(x => x.Id == id);
                if (occ == null)
                {
                    TempData["Error"] = "Occurrence not found.";
                    return RedirectToAction(nameof(Index), new { condominiumId });
                }

                list.RemoveAll(x => x.Id == id);
                await _store.WriteAllAsync(list);

                TempData["Success"] = "Occurrence deleted.";
                return RedirectToAction(nameof(Index), new { condominiumId = condominiumId ?? occ.CondominiumId });
            }
            catch
            {
                TempData["Error"] = "An unexpected error occurred while deleting the occurrence.";
                return RedirectToAction(nameof(Index), new { condominiumId });
            }
        }


    }
}

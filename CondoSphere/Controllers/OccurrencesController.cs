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
        private readonly JsonFileStore<Occurrence> _store;
        private readonly DomainNotificationService _notify;
        private readonly ICondominiumRepository _condos;
        private readonly IUnitRepository _units;
        public OccurrencesController(IWebHostEnvironment env,
                                     DomainNotificationService notify,
                                      ICondominiumRepository condos,
                                        IUnitRepository units)
        {
            _store = new JsonFileStore<Occurrence>(env, "appdata/occurrences.json");
            _notify = notify;
            _condos = condos;
            _units = units;
        }

        public async Task<IActionResult> Index(int? condominiumId)
        {
            var list = await _store.ReadAllAsync();
            if (condominiumId.HasValue)
                list = list.Where(x => x.CondominiumId == condominiumId.Value).ToList();

            if (User.IsInRole("Resident"))
            {
                var me = GetCurrentEmail();
                if (!string.IsNullOrEmpty(me))
                    list = list
                        .Where(x => ((x.CreatedBy ?? string.Empty).Trim().ToLowerInvariant()) == me)
                        .ToList();
            }

            // Mapa para mostrar nomes em vez do Id (Company)                       
            var lookup = (await _condos.GetAllWithCompanyAsync())
                .ToDictionary(c => c.Id, c => (Condo: c.Name, Company: c.Company?.Name));
            ViewBag.CondoLookup = lookup;

            ViewBag.CurrentCondoId = condominiumId;
            ViewBag.IsStaff = User.IsInRole("Administrator") || User.IsInRole("Manager");

            return View(list.OrderByDescending(x => x.CreatedAt).ToList());
        }


        // e-mail normalizado do utilizador logado
        private string GetCurrentEmail()
            => (User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? string.Empty)
                .Trim().ToLowerInvariant();

        // ids de condomínios onde o RESIDENTE tem unidade
        private async Task<HashSet<int>> GetAllowedCondoIdsForCurrentResidentAsync()
        {
            var set = new HashSet<int>();
            if (!User.IsInRole("Resident")) return set;

            var meId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var meEmail = GetCurrentEmail();

            // usa o seu IUnitRepository existente
            var allUnits = await _units.GetAllAsync(); // não remova nada que já tenha
            foreach (var u in allUnits)
            {
                var byId = !string.IsNullOrEmpty(u.OwnerId) && u.OwnerId == meId;
                var byEmail = !string.IsNullOrEmpty(u?.Owner?.Email) &&
                              string.Equals(u.Owner.Email.Trim(), meEmail, StringComparison.OrdinalIgnoreCase);
                if (byId || byEmail) set.Add(u.CondominiumId);
            }
            return set;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? condominiumId)
        {
            ViewBag.CondoId = condominiumId;

            if (User.IsInRole("Resident"))
            {
                var allowed = await GetAllowedCondoIdsForCurrentResidentAsync();
                await LoadCondominiumsSelectAsync(condominiumId, allowed); // overload abaixo
            }
            else
            {
                await LoadCondominiumsSelectAsync(condominiumId, null);
            }

            return View();
        }

        // overload com filtro
        private async Task LoadCondominiumsSelectAsync(int? selectedId, HashSet<int>? allowedIds)
        {
            var all = await _condos.GetAllWithCompanyAsync();
            if (allowedIds != null && allowedIds.Count > 0)
                all = all.Where(c => allowedIds.Contains(c.Id)).ToList();

            var items = all
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Company != null ? $"{c.Name} — {c.Company.Name}" : c.Name
                })
                .ToList();

            ViewBag.CondominiumId = new SelectList(items, "Value", "Text", selectedId?.ToString());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Occurrence model)
        {
            // para repovoar o dropdown corretamente quando der erro de validação
            HashSet<int>? allowed = null;
            if (User.IsInRole("Resident"))
                allowed = await GetAllowedCondoIdsForCurrentResidentAsync();

            if (!ModelState.IsValid)
            {
                // usa o overload que aceita filtro (se já criou conforme combinamos)
                await LoadCondominiumsSelectAsync(model.CondominiumId, allowed);
                return View(model);
            }

            // --- regras para residente ---
            if (User.IsInRole("Resident"))
            {
                // condomínio tem de estar na lista permitida
                if (allowed == null || !allowed.Contains(model.CondominiumId))
                    return Forbid();

                // força CreatedBy = e-mail do utilizador logado
                model.CreatedBy = GetCurrentEmail();
            }
            else
            {
                // staff: normaliza e-mail se foi preenchido
                if (!string.IsNullOrWhiteSpace(model.CreatedBy))
                    model.CreatedBy = model.CreatedBy.Trim().ToLowerInvariant();
            }

            // gravação
            var list = await _store.ReadAllAsync();
            model.CreatedAt = DateTime.UtcNow;
            list.Add(model);
            await _store.WriteAllAsync(list);

            // notificação
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


        // ---------------- helpers ----------------                                 
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
            if (User.IsInRole("Resident"))
            {
                var allowed = await GetAllowedCondoIdsForCurrentResidentAsync();
                if (!allowed.Contains(condominiumId))
                    return Json(Array.Empty<object>()); 
            }

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

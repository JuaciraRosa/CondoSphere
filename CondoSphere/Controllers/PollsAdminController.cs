using CondoSphere.Data;
using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using CondoSphere.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CondoSphere.Controllers
{
    [Authorize]
    public class PollsAdminController : Controller
    {
        private readonly IPollRepository _polls;
        private readonly ICondominiumRepository _condos;
        private readonly UserManager<User> _userManager;

        public PollsAdminController(
            IPollRepository polls,
            ICondominiumRepository condos,
            UserManager<User> userManager)
        {
            _polls = polls;
            _condos = condos;
            _userManager = userManager;
        }

        // Lista do admin (por criador)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index(int? condoId = null)
        {
            if (condoId.HasValue)
            {
                var list = await _polls.GetOpenByCondoAsync(condoId.Value);
                return View(list);
            }

            var userId = _userManager.GetUserId(User)!;
            var mine = await _polls.GetByCreatorAsync(userId);
            return View(mine);
        }

        [HttpGet]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Create()
        {
            await LoadCondominiumsSelectAsync();
            return View(new PollCreateVm { Options = new List<string> { "", "" } }); // 2 campos iniciais
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Create(PollCreateVm vm)
        {
            if (vm.EndsAtUtc <= vm.StartsAtUtc)
                ModelState.AddModelError(nameof(vm.EndsAtUtc), "End date must be after start date.");

            var cleanOptions = (vm.Options ?? new())
                .Select(o => (o ?? "").Trim())
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (cleanOptions.Count < 2)
                ModelState.AddModelError(nameof(vm.Options), "Please provide at least two options.");

            if (!ModelState.IsValid)
            {
                await LoadCondominiumsSelectAsync(vm.CondominiumId);
                return View(vm);
            }

            var userId = _userManager.GetUserId(User)!;
            var poll = new Poll
            {
                Title = vm.Title,
                Description = vm.Description,
                CondominiumId = vm.CondominiumId,
                StartsAtUtc = vm.StartsAtUtc,
                EndsAtUtc = vm.EndsAtUtc,
                AllowSingleChoice = vm.AllowSingleChoice,
                CreatedById = userId
            };

            poll.Options = cleanOptions.Select(txt => new PollOption { Text = txt }).ToList();

            await _polls.AddAsync(poll);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Edit(int id)
        {
            var db = await _polls.GetDetailsAsync(id);
            if (db == null) return NotFound();

            var vm = new PollEditVm
            {
                Id = db.Id,
                Title = db.Title,
                Description = db.Description,
                CondominiumId = db.CondominiumId ?? 0,
                StartsAtUtc = db.StartsAtUtc,
                EndsAtUtc = db.EndsAtUtc,
                AllowSingleChoice = db.AllowSingleChoice,
                ExistingOptions = db.Options.OrderBy(o => o.Id).ToList(),
                NewOptions = new List<string>()
            };

            await LoadCondominiumsSelectAsync(vm.CondominiumId);
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Edit(PollEditVm vm)
        {
            if (vm.EndsAtUtc <= vm.StartsAtUtc)
                ModelState.AddModelError(nameof(vm.EndsAtUtc), "End date must be after start date.");

            if (!ModelState.IsValid)
            {
                await LoadCondominiumsSelectAsync(vm.CondominiumId);
                return View(vm);
            }

            // Monta um Poll "rasa" só com os campos a atualizar
            var patch = new Poll
            {
                Id = vm.Id,
                Title = vm.Title,
                Description = vm.Description,
                CondominiumId = vm.CondominiumId,
                StartsAtUtc = vm.StartsAtUtc,
                EndsAtUtc = vm.EndsAtUtc,
                AllowSingleChoice = vm.AllowSingleChoice
            };

            await _polls.UpdateAsync(patch, vm.NewOptions ?? new List<string>());
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Close(int id)
        {
            await _polls.CloseAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            await _polls.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadCondominiumsSelectAsync(int? selectedId = null)
        {
            var condos = (await _condos.GetAllAsync())
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToList();

            ViewBag.CondominiumId = new SelectList(condos, "Value", "Text", selectedId?.ToString());
        }
    }
}

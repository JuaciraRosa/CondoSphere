using CondoSphere.Data.Interfaces;
using CondoSphere.Messaging;
using CondoSphere.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace CondoSphere.Controllers
{
    [Authorize(Roles = "Administrator,Manager,Resident")]
    public class MaintenanceRequestsController : Controller
    {
        private readonly IMaintenanceRequestRepository _requests;
        private readonly ICondominiumRepository _condos;
        private readonly IUserRepository _users;
        private readonly DomainNotificationService _notify;

        public MaintenanceRequestsController(
            IMaintenanceRequestRepository requests,
            ICondominiumRepository condos,
            IUserRepository users,
            DomainNotificationService notify)
        {
            _requests = requests;
            _condos = condos;
            _users = users;
            _notify = notify;
        }

        // GET: MaintenanceRequests
        public async Task<IActionResult> Index()
        {
            var items = await _requests.GetAllDetailedAsync(); // inclui Condominium + SubmittedBy
            var me = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (User.IsInRole("Resident"))
                items = items.Where(r => r.SubmittedById == me);

            return View(items);
        }

        // GET: MaintenanceRequests/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var req = await _requests.GetByIdDetailedAsync(id);
            if (req == null) return NotFound();

            if (User.IsInRole("Resident") && req.SubmittedById != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            return View(req);
        }

        // GET: MaintenanceRequests/Create
        public async Task<IActionResult> Create()
        {
            await PopulateSelectsAsync();
            return View(new MaintenanceRequest
            {
                SubmittedAt = DateTime.UtcNow,
                Status = RequestStatus.Open
            });
        }

        // POST: MaintenanceRequests/Create
      
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MaintenanceRequest model)
        {
            if (User.IsInRole("Resident"))
                model.SubmittedById = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (model.SubmittedAt == default)
                model.SubmittedAt = DateTime.UtcNow;

            if (!ModelState.IsValid)
            {
                await PopulateSelectsAsync(model.SubmittedById, model.CondominiumId);
                return View(model);
            }

            await _requests.AddAsync(model);
            await _requests.SaveChangesAsync();

            // e-mail de confirmação
            var submitter = await _users.GetByIdAsync(model.SubmittedById);
            var condo = await _condos.GetByIdAsync(model.CondominiumId);
            await _notify.MaintenanceRequestReceivedAsync(
                to: submitter?.Email ?? string.Empty,
                title: model.Title,
                requestId: model.Id,
                condoName: condo?.Name ?? "#"
            );

            return RedirectToAction(nameof(Index));
        }


        // GET: MaintenanceRequests/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var req = await _requests.GetByIdDetailedAsync(id);
            if (req == null) return NotFound();

            if (User.IsInRole("Resident") && req.SubmittedById != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            await PopulateSelectsAsync(req.SubmittedById, req.CondominiumId);
            return View(req);
        }

        // POST: MaintenanceRequests/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MaintenanceRequest model)
        {
            if (id != model.Id) return NotFound();

            // carrega a entidade rastreada SEM Includes (não precisamos deles para editar)
            var entity = await _requests.GetByIdAsync(id);
            if (entity == null) return NotFound();

            // Resident não pode trocar SubmittedById
            if (User.IsInRole("Resident"))
                model.SubmittedById = entity.SubmittedById;

            if (!ModelState.IsValid)
            {
                await PopulateSelectsAsync(model.SubmittedById, model.CondominiumId);
                return View(model);
            }


            var oldStatus = entity.Status;
            // mapeia apenas os campos que podem mesmo ser alterados
            entity.Title = model.Title;
            entity.Description = model.Description;
            entity.Status = model.Status;
            entity.SubmittedAt = model.SubmittedAt;   // ou preserve entity.SubmittedAt se não quiser editar
            entity.CondominiumId = model.CondominiumId;
            entity.SubmittedById = model.SubmittedById; // só Admin/Manager; Resident já foi travado acima

            // como 'entity' já está rastreada, só gravar
            await _requests.SaveChangesAsync();


            if (oldStatus != entity.Status)
            {
                try
                {
                    var to = await _requests.GetRequesterEmailAsync(entity.SubmittedById);
                    if (!string.IsNullOrWhiteSpace(to))
                    {
                        await _notify.MaintenanceRequestStatusChangedAsync(
                            to, entity, oldStatus.ToString()); // usa o método de 3 argumentos
                    }
                }
                catch
                {
                    // não quebrar o fluxo por falha de e-mail
                }
            }

            TempData["Success"] = "Pedido atualizado.";

            return RedirectToAction(nameof(Index));
        }


        // GET: MaintenanceRequests/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var req = await _requests.GetByIdDetailedAsync(id);
            if (req == null) return NotFound();

            if (User.IsInRole("Resident") && req.SubmittedById != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            return View(req);
        }

        // POST: MaintenanceRequests/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var req = await _requests.GetByIdAsync(id);
            if (req == null) return NotFound();

            if (User.IsInRole("Resident") && req.SubmittedById != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            await _requests.DeleteAsync(id);
            await _requests.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Helpers
        private async Task PopulateSelectsAsync(string? submittedById = null, int? condominiumId = null)
        {
            var condos = await _condos.GetAllAsync();
            ViewBag.CondominiumId = new SelectList(condos, "Id", "Name", condominiumId);

            var users = await _users.GetAllAsync();
            ViewBag.SubmittedById = new SelectList(
                users.Select(u => new { u.Id, Name = string.IsNullOrWhiteSpace(u.FullName) ? u.Email : u.FullName }),
                "Id", "Name", submittedById
            );
        }
    }
}

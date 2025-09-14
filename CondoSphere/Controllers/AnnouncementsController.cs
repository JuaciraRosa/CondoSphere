using CondoSphere.Data.Interfaces;
using CondoSphere.Messaging;
using CondoSphere.Models;
using CondoSphere.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace CondoSphere.Controllers
{
    [Authorize]
    public class AnnouncementsController : Controller
    {
        private readonly IAnnouncementRepository _repo;
        private readonly ICondominiumRepository _condos;
        private readonly IWebHostEnvironment _env;
        private readonly DomainNotificationService _notify;
        private readonly IAnnouncementReadService _reads;

        public AnnouncementsController(
            IAnnouncementRepository repo,
            ICondominiumRepository condos,
            IWebHostEnvironment env,
            DomainNotificationService notify,
            IAnnouncementReadService reads)
        {
            _repo = repo; _condos = condos; _env = env; _notify = notify; _reads = reads; 
        }

        // Morador vê o que for global + do seu condomínio
        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> Index(int? condominiumId = null)
        {
            var list = await _repo.GetLatestAsync(condominiumId);
            ViewBag.CondoId = condominiumId;
            return View(list);
        }


        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var a = await _repo.GetByIdAsync(id);
            if (a == null) return NotFound();

            // marca como lido para o utilizador autenticado
            if (User?.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                    await _reads.MarkAsReadAsync(id, userId);
            }

            return View(a);
        }


        [Authorize(Roles = "Administrator,Manager")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadCondoSelectAsync();
            return View(new Announcement { SendEmail = true, SendInApp = true });
        }

        [Authorize(Roles = "Administrator,Manager")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Announcement a, IFormFile? file)
        {
            if (!ModelState.IsValid)
            {
                await LoadCondoSelectAsync(a.CondominiumId);
                return View(a);
            }

            if (file is { Length: > 0 })
                a.AttachmentPath = await SaveAttachmentAsync(file);

            await _repo.AddAsync(a);

            // Envia agora se não for agendado
            if (a.ScheduledAtUtc is null)
                _ = Task.Run(() => BroadcastAsync(a));

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrator,Manager")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var a = await _repo.GetByIdAsync(id);
            if (a == null) return NotFound();
            await LoadCondoSelectAsync(a.CondominiumId);
            return View(a);
        }

        [Authorize(Roles = "Administrator,Manager")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Announcement form, IFormFile? file)
        {
            if (id != form.Id) return NotFound();
            var a = await _repo.GetByIdAsync(id);
            if (a == null) return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadCondoSelectAsync(form.CondominiumId);
                return View(form);
            }

            a.Title = form.Title;
            a.Body = form.Body;
            a.CondominiumId = form.CondominiumId;
            a.SendEmail = form.SendEmail;
            a.SendInApp = form.SendInApp;
            a.ScheduledAtUtc = form.ScheduledAtUtc;

            if (file is { Length: > 0 })
                a.AttachmentPath = await SaveAttachmentAsync(file);

            _repo.Update(a);
            await _repo.SaveChangesAsync();

            // Se virou “enviar agora”, dispara
            if (a.ScheduledAtUtc is null)
                _ = Task.Run(() => BroadcastAsync(a));

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrator,Manager")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _repo.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // ===== helpers =====
        private async Task LoadCondoSelectAsync(int? selected = null)
        {
            var list = (await _condos.GetAllAsync())
                       .OrderBy(c => c.Name)
                       .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                       .ToList();
            list.Insert(0, new SelectListItem { Value = "", Text = "Todos os condomínios (global)" });
            ViewBag.CondominiumId = new SelectList(list, "Value", "Text", selected?.ToString());
        }

        private async Task<string> SaveAttachmentAsync(IFormFile file)
        {
            var folder = Path.Combine(_env.WebRootPath, "uploads", "ann");
            Directory.CreateDirectory(folder);
            var ext = Path.GetExtension(file.FileName);
            var name = $"{Guid.NewGuid():N}{ext}";
            var path = Path.Combine(folder, name);
            await using var fs = System.IO.File.Create(path);
            await file.CopyToAsync(fs);
            return $"/uploads/ann/{name}";
        }


        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> Download(int id)
        {
            var a = await _repo.GetByIdAsync(id);
            if (a == null || string.IsNullOrWhiteSpace(a.AttachmentPath))
                return NotFound();

            var rel = a.AttachmentPath.TrimStart('/');
            var full = Path.Combine(_env.WebRootPath, rel.Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(full)) return NotFound();

            var contentType = "application/octet-stream";
            var ext = Path.GetExtension(full).ToLowerInvariant();
            if (ext == ".pdf") contentType = "application/pdf";
            else if (ext == ".doc") contentType = "application/msword";
            else if (ext == ".docx") contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            var fileName = Path.GetFileName(full);
            return PhysicalFile(full, contentType, fileName); 
        }

        private async Task BroadcastAsync(Announcement a)
        {
            // 1) Notificação por e-mail
            if (a.SendEmail)
            {
                // Reaproveite sua infra atual: um e-mail para mailing list dos moradores
                await _notify.SendAnnouncementEmailAsync(
                    subject: $"[Comunicado] {a.Title}",
                    htmlBody: a.Body,
                    condoId: a.CondominiumId,
                    attachmentUrl: a.AttachmentPath);
            }

            // 2) In-App: reuse “Notifications”/sua hub ou apenas DB + contador
            if (a.SendInApp)
            {
                await _notify.PushInAppAsync(
                    title: a.Title,
                    message: a.Body,
                    condoId: a.CondominiumId,
                    deeplink: $"/Announcements/Details/{a.Id}");
            }
        }
    }
}

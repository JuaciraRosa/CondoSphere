using CondoSphere.Data.Interfaces;
using CondoSphere.Messaging;
using CondoSphere.Models;
using CondoSphere.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Net;
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
        private readonly IUserRepository _users;

        public AnnouncementsController(
            IAnnouncementRepository repo,
            ICondominiumRepository condos,
            IWebHostEnvironment env,
            DomainNotificationService notify,
            IAnnouncementReadService reads,
            IUserRepository users)
        {
            _repo = repo; _condos = condos; _env = env; _notify = notify; _reads = reads; _users = users;
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
            // se o seu repo não persiste aqui, descomente:
            // await _repo.SaveChangesAsync();

            var attachmentUrl = AbsoluteUrl(a.AttachmentPath);

            if (a.ScheduledAtUtc is null)
            {
                try
                {
                    var recipients = await GetAnnouncementRecipientsAsync(a.CondominiumId);
                    if (recipients.Count > 0)
                    {
                        await _notify.AnnouncementCreatedAsync(recipients, a, attachmentUrl);
                        TempData["Success"] = "Announcement created and notifications sent.";
                    }
                    else
                    {
                        TempData["Success"] = "Announcement created (no recipients found).";
                    }
                }
                catch (Exception ex)
                {
                    TempData["Success"] = $"Announcement created. (Warning: e-mail failed: {ex.Message})";
                }
            }
            else
            {
                TempData["Success"] = "Announcement scheduled.";
            }

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

            // snapshot (valores antigos) — AGORA depois do null-check
            var oldTitle = a.Title;
            var oldBody = a.Body;
            var oldCondoId = a.CondominiumId;
            var oldAttachment = a.AttachmentPath;

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

            bool contentChanged =
                !string.Equals(a.Title, oldTitle, StringComparison.Ordinal) ||
                !string.Equals(a.Body, oldBody, StringComparison.Ordinal) ||
                a.CondominiumId != oldCondoId;

            bool attachmentChanged =
                !string.Equals(a.AttachmentPath, oldAttachment, StringComparison.OrdinalIgnoreCase);

            if (contentChanged || attachmentChanged)
            {
                try
                {
                    var recipients = await GetAnnouncementRecipientsAsync(a.CondominiumId);
                    if (recipients.Count > 0)
                    {
                        await _notify.AnnouncementUpdatedAsync(
                            recipients,
                            a,
                            attachmentChanged,
                            AbsoluteUrl(a.AttachmentPath)
                        );
                        TempData["Success"] = "Announcement updated and notifications sent.";
                    }
                    else
                    {
                        TempData["Success"] = "Announcement updated.";
                    }
                }
                catch (Exception ex)
                {
                    TempData["Success"] = $"Announcement updated. (Warning: e-mail failed: {ex.Message})";
                }
            }
            else
            {
                TempData["Success"] = "Announcement updated.";
            }

            return RedirectToAction(nameof(Index));
        }


        [Authorize(Roles = "Administrator,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var a = await _repo.GetByIdAsync(id);
                if (a == null) return NotFound();

                var recipients = await GetAnnouncementRecipientsAsync(a.CondominiumId);

                await _repo.DeleteAsync(id);
                // se o seu repo não persiste aqui, descomente:
                // await _repo.SaveChangesAsync();

                if (recipients.Count > 0)
                {
                    try
                    {
                        await _notify.AnnouncementDeletedAsync(recipients, a);
                        TempData["Success"] = "Announcement deleted and cancellation sent.";
                    }
                    catch (Exception ex)
                    {
                        TempData["Success"] = $"Announcement deleted. (Warning: e-mail failed: {ex.Message})";
                    }
                }
                else
                {
                    TempData["Success"] = "Announcement deleted.";
                }
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Record could not be deleted because it is referenced by other data.";
            }
            catch
            {
                TempData["Error"] = "An unexpected error occurred while deleting the record.";
            }

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


        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var a = await _repo.GetByIdAsync(id);
            if (a == null || string.IsNullOrWhiteSpace(a.AttachmentPath)) return NotFound();

            var rel = a.AttachmentPath.TrimStart('/');
            var full = Path.Combine(_env.WebRootPath, rel.Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(full)) return NotFound();

            var contentType = "application/octet-stream";
            var ext = Path.GetExtension(full).ToLowerInvariant();
            if (ext == ".pdf") contentType = "application/pdf";
            else if (ext == ".doc") contentType = "application/msword";
            else if (ext == ".docx") contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            return PhysicalFile(full, contentType, Path.GetFileName(full));
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



        private async Task<List<string>> GetAnnouncementRecipientsAsync(int? condominiumId)
        {
            // Se for direcionado a um condomínio específico → e-mails dos proprietários desse condomínio
            if (condominiumId.HasValue)
                return await _condos.GetOwnerEmailsAsync(condominiumId.Value);

            // Caso contrário → todos os residentes ativos (ajuste se precisar limitar por empresa)
            var allUsers = await _users.GetAllAsync();
            return allUsers
                .Where(u => u.IsActive && u.Role == UserRole.Resident && !string.IsNullOrWhiteSpace(u.Email))
                .Select(u => u.Email!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private async Task BroadcastAsync(Announcement a, string? attachmentUrl)
        {
            var recipients = await GetAnnouncementRecipientsAsync(a.CondominiumId);
            if (recipients.Count == 0) return;

            // assunto / corpo simples (use seu template se preferir)
            var subject = string.IsNullOrWhiteSpace(a.Title) ? "Comunicado" : a.Title!;
            var body = $@"
        <h3>{WebUtility.HtmlEncode(a.Title)}</h3>
        <p>{(string.IsNullOrWhiteSpace(a.Body) ? "" : WebUtility.HtmlEncode(a.Body).Replace("\n", "<br/>"))}</p>";

            await _notify.SendAnnouncementEmailAsync(
                subject: subject,
                htmlBody: body,
                condoId: a.CondominiumId,
                attachmentUrl: attachmentUrl,
                recipients: recipients
            );
        }

        // AnnouncementsController
        private string? AbsoluteUrl(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;

            // Se já for absoluta, devolve como está
            if (Uri.IsWellFormedUriString(path, UriKind.Absolute)) return path;

            var req = HttpContext?.Request;
            if (req == null) return path;

            var baseUrl = $"{req.Scheme}://{req.Host}";
            var p = path.StartsWith("~") ? path[1..] : path;
            if (!p.StartsWith("/")) p = "/" + p;

            return baseUrl + p;
        }


    }
}

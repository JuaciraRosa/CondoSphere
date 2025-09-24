using CondoSphere.Data;
using CondoSphere.Data.Interfaces;
using CondoSphere.Messaging;
using CondoSphere.Models;
using CondoSphere.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Configuration.Provider;
using System.Linq;
using System.Threading.Tasks;

namespace CondoSphere.Controllers
{
    [Authorize]
    public class MeetingsController : Controller
    {
        private readonly IMeetingRepository _meetings;
        private readonly ICondominiumRepository _condos;
        private readonly IWebHostEnvironment _env;
        private readonly IOnlineMeetingProviderFactory _providers;
        private readonly DomainNotificationService _notify;
        private readonly ILogger<MeetingsController> _logger;

        public MeetingsController(
            IMeetingRepository meetings,
            ICondominiumRepository condos,
            IWebHostEnvironment env,
            IOnlineMeetingProviderFactory providers,
            DomainNotificationService notify,
              ILogger<MeetingsController> logger)
        {
            _meetings = meetings;
            _condos = condos;
            _env = env;
            _providers = providers;
            _notify = notify;
            _logger = logger;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var list = await _meetings.GetAllWithCondoAsync(); 
            return View(list);
        }


        [AllowAnonymous]

        public async Task<IActionResult> Details(int id)
        {
            var meeting = await _meetings.GetByIdWithCondoAsync(id);
            if (meeting == null) return NotFound();
            return View(meeting);
        }

      
         [Authorize(Roles = "Administrator,Manager")]
         [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadCondominiumsSelectAsync();
            return View(new Meeting { ScheduledDate = DateTime.Now.AddDays(1) });
        }

        [Authorize(Roles = "Administrator,Manager")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Meeting meeting)
        {
            if (!ModelState.IsValid)
            {
                await LoadCondominiumsSelectAsync(meeting.CondominiumId);
                return View(meeting);
            }

            // upload opcional
            await SaveMinutesFileAsync(meeting);


            // dentro do POST Create
            if (meeting.IsOnline)
            {
                meeting.OnlineProvider = string.IsNullOrWhiteSpace(meeting.OnlineProvider) ? "Google" : meeting.OnlineProvider;

                var prov = _providers.Get(meeting.OnlineProvider);
                try
                {
                    var res = await prov.CreateAsync(meeting);
                    meeting.OnlineProvider = res.Provider;
                    meeting.OnlineMeetingId = res.ExternalId;
                    meeting.OnlineJoinUrl = res.JoinUrl;
                    meeting.OnlineStartUrl = res.StartUrl;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Falha ao criar reunião online: {ex.Message}");
                    await LoadCondominiumsSelectAsync(meeting.CondominiumId);
                    return View(meeting);
                }
            }


            await _meetings.AddAsync(meeting);
            await _meetings.SaveChangesAsync(); // ensure Id is generated
                                                // Build attachment download URL if exists
            string? attachmentUrl = null;
            if (!string.IsNullOrWhiteSpace(meeting.MinutesDocumentPath))
            {
                attachmentUrl = Url.Action("Download", "Meetings", new { id = meeting.Id }, Request.Scheme);
            }

            // Notify owners por e-mail
            try
            {
                var emails = await _condos.GetOwnerEmailsAsync(meeting.CondominiumId);
                if (emails.Count > 0)
                {
                    if (!string.IsNullOrWhiteSpace(attachmentUrl))
                        await _notify.MeetingScheduledAsync(emails, meeting, attachmentUrl);
                    else
                        await _notify.MeetingScheduledAsync(emails, meeting);

                    TempData["Success"] = $"Meeting created and {emails.Count} residents notified by email.";
                }
                else
                {
                    TempData["Success"] = "Meeting created (no resident e-mails found).";
                }
            }
            catch (Exception ex)
            {
                TempData["Success"] = $"Meeting created. (Warning: failed sending e-mails: {ex.Message})";
            }

            // SMS de TESTE (número hardcoded)
            // mantém, mas não deixa o fluxo quebrar se falhar
            try
            {
                await _notify.NotifyResidentAsync(
                    "+351937793133",
                    "CondoSphere: Foi marcada uma reunião. Verifique o site."
                );
            }
            catch (Exception ex)
            {
                // registre se quiser, mas não mude a mensagem de sucesso do e-mail
                _logger.LogWarning(ex, "Falha ao enviar SMS de teste.");
            }

            // redireciona normalmente
            return RedirectToAction(nameof(Index));

        }



        [Authorize(Roles = "Administrator,Manager")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var meeting = await _meetings.GetByIdAsync(id);
            if (meeting == null) return NotFound();

            await LoadCondominiumsSelectAsync(meeting.CondominiumId);
            return View(meeting);
        }


        [Authorize(Roles = "Administrator,Manager")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Meeting meeting)
        {
            if (id != meeting.Id) return NotFound();

            var existingMeeting = await _meetings.GetByIdAsync(id);
            if (existingMeeting == null) return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadCondominiumsSelectAsync(meeting.CondominiumId);
                return View(meeting);
            }

            // Valores antigos (para decidir se avisa os moradores)
            var oldDate = existingMeeting.ScheduledDate;
            var oldAgenda = existingMeeting.Agenda;
            var oldCondoId = existingMeeting.CondominiumId;
            var oldJoinUrl = existingMeeting.OnlineJoinUrl;
            var oldIsOnline = existingMeeting.IsOnline;
            var oldMinutes = existingMeeting.MinutesDocumentPath;




            if (meeting.MinutesFile != null && meeting.MinutesFile.Length > 0)
            {
                await SaveMinutesFileAsync(meeting, replaceExisting: true);
            }
            else
            {
                meeting.MinutesDocumentPath = existingMeeting.MinutesDocumentPath;
            }

            // Atualiza os demais campos
            existingMeeting.ScheduledDate = meeting.ScheduledDate;
            existingMeeting.Agenda = meeting.Agenda;
            existingMeeting.CondominiumId = meeting.CondominiumId;
            existingMeeting.MinutesDocumentPath = meeting.MinutesDocumentPath;



            if (meeting.IsOnline)
            {
                if (string.Equals(meeting.OnlineProvider, "GoogleManual", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(meeting.OnlineJoinUrl))
                    {
                        ModelState.AddModelError("OnlineJoinUrl", "Informe o link do Google Meet.");
                        await LoadCondominiumsSelectAsync(meeting.CondominiumId);
                        return View(meeting);
                    }

                    existingMeeting.IsOnline = true;
                    existingMeeting.OnlineProvider = "GoogleManual";
                    existingMeeting.OnlineMeetingId = null;
                    existingMeeting.OnlineJoinUrl = meeting.OnlineJoinUrl; // do formulário
                    existingMeeting.OnlineStartUrl = null;
                }
                else
                {
                    var trocouDeProvider = !string.Equals(existingMeeting.OnlineProvider, meeting.OnlineProvider, StringComparison.OrdinalIgnoreCase)
                                           || string.IsNullOrWhiteSpace(existingMeeting.OnlineMeetingId);

                    if (trocouDeProvider)
                    {
                        var prov = _providers.Get(meeting.OnlineProvider ?? existingMeeting.OnlineProvider ?? "Zoom");
                        var res = await prov.CreateAsync(meeting);
                        existingMeeting.IsOnline = true;
                        existingMeeting.OnlineProvider = res.Provider;
                        existingMeeting.OnlineMeetingId = res.ExternalId;
                        existingMeeting.OnlineJoinUrl = res.JoinUrl;
                        existingMeeting.OnlineStartUrl = res.StartUrl;
                    }
                    else
                    {
                        existingMeeting.IsOnline = true; // mantém dados online já criados
                    }
                }
            }
            else
            {
                // desligou online
                existingMeeting.IsOnline = false;
                existingMeeting.OnlineProvider = null;
                existingMeeting.OnlineMeetingId = null;
                existingMeeting.OnlineJoinUrl = null;
                existingMeeting.OnlineStartUrl = null;
            }




            _meetings.Update(existingMeeting);
            await _meetings.SaveChangesAsync();


            // Decidir se vale avisar
            bool changed =
                existingMeeting.ScheduledDate != oldDate ||
                existingMeeting.Agenda != oldAgenda ||
                existingMeeting.CondominiumId != oldCondoId ||
                existingMeeting.IsOnline != oldIsOnline ||
                existingMeeting.OnlineJoinUrl != oldJoinUrl;

            // foi adicionado um anexo nesta edição?
            bool attachmentAdded =
                string.IsNullOrEmpty(oldMinutes) &&
                !string.IsNullOrWhiteSpace(existingMeeting.MinutesDocumentPath);

            // se tiver anexo novo, prepara link de download
            string? attachmentUrl = attachmentAdded
                ? Url.Action("Download", "Meetings", new { id = existingMeeting.Id }, Request.Scheme)
                : null;

            if (changed || attachmentAdded)
            {
                try
                {
                    var emails = await _condos.GetOwnerEmailsAsync(existingMeeting.CondominiumId);
                    if (emails.Count > 0)
                    {
                        await _notify.MeetingUpdatedAsync(emails, existingMeeting, attachmentAdded, attachmentUrl);
                        TempData["Success"] = $"Reunião atualizada e {emails.Count} moradores notificados.";
                    }
                    else
                    {
                        TempData["Success"] = "Reunião atualizada.";
                    }
                }
                catch (Exception ex)
                {
                    TempData["Success"] = $"Reunião atualizada. (Aviso: falha ao enviar e-mails: {ex.Message})";
                }
            }
            else
            {
                TempData["Success"] = "Reunião atualizada.";
            }

            return RedirectToAction(nameof(Index));

        }


        // GET: Meetings/Delete/{id}
        [Authorize(Roles = "Administrator,Manager")]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var meeting = await _meetings.GetByIdWithCondoAsync(id);
            if (meeting == null) return NotFound();
            return View(meeting);
        }

        // POST: Meetings/Delete/{id}
        [Authorize(Roles = "Administrator,Manager")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var meeting = await _meetings.GetByIdAsync(id);
                if (meeting == null)
                {
                    TempData["Error"] = "Meeting not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Notify cancellation BEFORE deleting
                try
                {
                    var emails = await _condos.GetOwnerEmailsAsync(meeting.CondominiumId);
                    if (emails.Count > 0)
                        await _notify.MeetingCanceledAsync(emails, meeting);
                }
                catch { /* don't block delete on email failure */ }

                // Delete physical file then record
                DeletePhysicalFileIfExists(meeting.MinutesDocumentPath);
                await _meetings.DeleteAsync(id);
                await _meetings.SaveChangesAsync();

                TempData["Success"] = "Meeting deleted successfully (canceled e-mail sent).";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Meeting could not be deleted due to related records.";
            }
            catch
            {
                TempData["Error"] = "An unexpected error occurred while deleting the meeting.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ===== helper (padrão) =====
        private async Task LoadCondominiumsSelectAsync(int? selectedId = null)
        {
            var condos = (await _condos.GetAllAsync())
                         .OrderBy(c => c.Name)
                         .Select(c => new SelectListItem
                         {
                             Value = c.Id.ToString(),
                             Text = c.Name
                         })
                         .ToList();

            ViewBag.CondominiumId = new SelectList(condos, "Value", "Text", selectedId?.ToString());
            ViewData["HasCondos"] = condos.Any();
        }



        // DOWNLOAD
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var meeting = await _meetings.GetByIdAsync(id);
            if (meeting == null || string.IsNullOrWhiteSpace(meeting.MinutesDocumentPath))
                return NotFound();

            var rel = meeting.MinutesDocumentPath.TrimStart('/');
            var full = Path.Combine(_env.WebRootPath, rel.Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(full)) return NotFound();

            var contentType = "application/octet-stream";
            var ext = Path.GetExtension(full).ToLowerInvariant();
            if (ext == ".pdf") contentType = "application/pdf";
            else if (ext == ".doc") contentType = "application/msword";
            else if (ext == ".docx") contentType =
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            return PhysicalFile(full, contentType, Path.GetFileName(full));
        }


        // ===== helpers =====

        private async Task SaveMinutesFileAsync(Meeting meeting, bool replaceExisting = false)
        {
            var file = meeting.MinutesFile;
            if (file == null || file.Length == 0) return;

            // validações
            var allowed = new[] { ".pdf", ".doc", ".docx" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
            {
                ModelState.AddModelError("MinutesFile", "Só é permitido PDF/DOC/DOCX.");
                return;
            }
            const long maxBytes = 5 * 1024 * 1024; // 5 MB
            if (file.Length > maxBytes)
            {
                ModelState.AddModelError("MinutesFile", "O ficheiro não pode exceder 5 MB.");
                return;
            }
            if (!ModelState.IsValid) return;

            // pasta
            var folder = Path.Combine(_env.WebRootPath, "uploads", "meetings");
            Directory.CreateDirectory(folder);

            // nome seguro e único
            var baseName = Path.GetFileNameWithoutExtension(file.FileName);
            var safeBase = string.Join("_", baseName.Split(Path.GetInvalidFileNameChars()))
                             .Trim('_');
            var fileName = $"minutes_{meeting.CondominiumId}_{DateTime.UtcNow:yyyyMMddHHmmss}_{safeBase}{ext}";
            var fullPath = Path.Combine(folder, fileName);

            // substituir?
            if (replaceExisting)
                DeletePhysicalFileIfExists(meeting.MinutesDocumentPath);

            // gravar
            using (var stream = System.IO.File.Create(fullPath))
                await file.CopyToAsync(stream);

            // caminho relativo para a BD
            meeting.MinutesDocumentPath = $"/uploads/meetings/{fileName}";
        }

        private void DeletePhysicalFileIfExists(string? relPath)
        {
            if (string.IsNullOrWhiteSpace(relPath)) return;
            var rel = relPath.TrimStart('/');
            var full = Path.Combine(_env.WebRootPath, rel.Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(full))
                System.IO.File.Delete(full);
        }
    }

}

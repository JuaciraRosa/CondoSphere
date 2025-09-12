using CondoSphere.Data;
using CondoSphere.Data.Interfaces;
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

        public MeetingsController(
            IMeetingRepository meetings,
            ICondominiumRepository condos,
            IWebHostEnvironment env,
            IOnlineMeetingProviderFactory providers)
        {
            _meetings = meetings;
            _condos = condos;
            _env = env;
            _providers = providers;
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


            // 2) se for online, cria no provedor
            if (meeting.IsOnline)
            {
                meeting.OnlineProvider = string.IsNullOrWhiteSpace(meeting.OnlineProvider) ? "Zoom" : meeting.OnlineProvider;
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
            return RedirectToAction(nameof(Index));
        }
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



            // Edit
            if (meeting.IsOnline)
            {
                var providerName = string.IsNullOrWhiteSpace(meeting.OnlineProvider)
                    ? (existingMeeting.OnlineProvider ?? "Zoom")
                    : meeting.OnlineProvider;

                var precisaCriar = string.IsNullOrWhiteSpace(existingMeeting.OnlineMeetingId)
                                || !string.Equals(existingMeeting.OnlineProvider, providerName, StringComparison.OrdinalIgnoreCase);

                if (precisaCriar)
                {
                    var prov = _providers.Get(providerName);
                    try
                    {
                        var res = await prov.CreateAsync(meeting);
                        existingMeeting.IsOnline = true;
                        existingMeeting.OnlineProvider = providerName;
                        existingMeeting.OnlineMeetingId = res.ExternalId;
                        existingMeeting.OnlineJoinUrl = res.JoinUrl;
                        existingMeeting.OnlineStartUrl = res.StartUrl;
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", $"Falha ao criar reunião online: {ex.Message}");
                        await LoadCondominiumsSelectAsync(meeting.CondominiumId);
                        return View(meeting);
                    }
                }
                else
                {
                    existingMeeting.IsOnline = true;
                }
            }
            else
            {
                existingMeeting.IsOnline = false;
                existingMeeting.OnlineProvider = null;
                existingMeeting.OnlineMeetingId = null;
                existingMeeting.OnlineJoinUrl = null;
                existingMeeting.OnlineStartUrl = null;
            }



            _meetings.Update(existingMeeting);
            await _meetings.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var meeting = await _meetings.GetByIdWithCondoAsync(id);
            if (meeting == null) return NotFound();
            return View(meeting);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var meeting = await _meetings.GetByIdAsync(id);
            if (meeting != null)
            {
                DeletePhysicalFileIfExists(meeting.MinutesDocumentPath);
                await _meetings.DeleteAsync(id);
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

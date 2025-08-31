using CondoSphere.Data.Interfaces;
using CondoSphere.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CondoSphere.Controllers
{
    [Authorize(Roles = "Administrator,Manager")]
    public class MeetingMinutesController : Controller
    {
        private readonly IMeetingRepository _meetings;
        private readonly IWebHostEnvironment _env;
        private readonly DomainNotificationService _notify;

        public MeetingMinutesController(IMeetingRepository meetings,
                                        IWebHostEnvironment env,
                                        DomainNotificationService notify)
        {
            _meetings = meetings;
            _env = env;
            _notify = notify;
        }

        // /MeetingMinutes/Manage?meetingId=123
        [HttpGet]
        public async Task<IActionResult> Manage(int meetingId)
        {
            var meeting = await _meetings.GetByIdAsync(meetingId);
            if (meeting == null) return NotFound();
            return View(meeting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int meetingId, IFormFile file)
        {
            var meeting = await _meetings.GetByIdAsync(meetingId);
            if (meeting == null) return NotFound();
            if (file == null || file.Length == 0)
                return RedirectToAction(nameof(Manage), new { meetingId });

            var folder = Path.Combine(_env.WebRootPath, "uploads", "meetings");
            Directory.CreateDirectory(folder);

            var fileName = $"minutes_{meetingId}_{Path.GetFileName(file.FileName)}";
            var fullPath = Path.Combine(folder, fileName);
            using (var stream = System.IO.File.Create(fullPath))
                await file.CopyToAsync(stream);

            meeting.MinutesDocumentPath = $"/uploads/meetings/{fileName}";
            _meetings.Update(meeting);
            await _meetings.SaveChangesAsync();


            // URL absoluta para incluir no e-mail
            var publicUrl = Url.Action("Download", "MeetingMinutes",
                new { meetingId }, Request.Scheme);

            // ENVIO DE E-MAIL:
            // Aqui, se tiveres um repositório de residentes, substitui este “to” por cada email dos residentes.
            var to = "Support@condosphere-web-app.somee.com"; // coloca a mailbox que configuraste
            await _notify.MeetingMinutesPublishedAsync(to, meeting.Id, publicUrl);

            TempData["ok"] = "Ata carregada e e-mail enviado.";
            return RedirectToAction(nameof(Manage), new { meetingId });
        }

        [AllowAnonymous]
        [HttpGet("/MeetingMinutes/Download/{meetingId}")]
        public async Task<IActionResult> Download(int meetingId)
        {
            var meeting = await _meetings.GetByIdAsync(meetingId);
            if (meeting == null || string.IsNullOrWhiteSpace(meeting.MinutesDocumentPath)) return NotFound();

            var path = meeting.MinutesDocumentPath;
            if (path.StartsWith("/")) path = path.Substring(1);
            var full = Path.Combine(_env.WebRootPath, path.Replace('/', Path.DirectorySeparatorChar));

            if (!System.IO.File.Exists(full)) return NotFound();
            var contentType = "application/pdf";
            return PhysicalFile(full, contentType, Path.GetFileName(full));
        }
    }
}

using CondoSphere.Data.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CondoSphere.API
{
    [Route("api/meeting-documents")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class MeetingDocumentsApiController : ControllerBase
    {
        private readonly IMeetingRepository _meetings;
        private readonly IWebHostEnvironment _env;

        public MeetingDocumentsApiController(IMeetingRepository meetings, IWebHostEnvironment env)
        {
            _meetings = meetings;
            _env = env;
        }

        // GET api/meeting-documents?condominiumId=1
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> List([FromQuery] int condominiumId)
        {
            var all = await _meetings.GetAllAsync();
            var data = all
                .Where(m => m.CondominiumId == condominiumId)
                .Select(m => new
                {
                    m.Id,
                    m.ScheduledDate,
                    m.Agenda,
                    HasDocument = !string.IsNullOrWhiteSpace(m.MinutesDocumentPath),
                    DownloadUrl = string.IsNullOrWhiteSpace(m.MinutesDocumentPath) ? null : $"/MeetingMinutes/Download/{m.Id}"
                })
                .OrderByDescending(x => x.ScheduledDate)
                .ToList();
            return Ok(data);
        }

        // POST api/meeting-documents/{meetingId}  (multipart/form-data)
        [HttpPost("{meetingId:int}")]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Upload(int meetingId, IFormFile file)
        {
            var meeting = await _meetings.GetByIdAsync(meetingId);
            if (meeting == null) return NotFound();
            if (file == null || file.Length == 0) return BadRequest("No file.");

            var folder = Path.Combine(_env.WebRootPath, "uploads", "meetings");
            Directory.CreateDirectory(folder);

            var fileName = $"minutes_{meetingId}_{Path.GetFileName(file.FileName)}";
            var fullPath = Path.Combine(folder, fileName);
            using (var stream = System.IO.File.Create(fullPath))
                await file.CopyToAsync(stream);

            meeting.MinutesDocumentPath = $"/uploads/meetings/{fileName}";
            _meetings.Update(meeting);
            await _meetings.SaveChangesAsync();


            return Ok(new { meeting.Id, meeting.MinutesDocumentPath });
        }
    }
}

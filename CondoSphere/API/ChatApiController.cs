using CondoSphere.Data;
using CondoSphere.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CondoSphere.API
{
    [ApiController]
    [Route("api/chat")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ChatApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ChatApiController(ApplicationDbContext db) => _db = db;

        // Cria um thread (morador)
        [HttpPost("threads")]
        public async Task<IActionResult> CreateThread([FromBody] CreateThreadReq req)
        {
            var userId = User?.Identity?.Name ?? User?.FindFirst("sub")?.Value ?? User?.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var t = new ChatThread
            {
                ResidentId = userId,
                CondominiumId = req.CondominiumId,
                Subject = string.IsNullOrWhiteSpace(req.Subject) ? "Suporte" : req.Subject.Trim()
            };
            _db.ChatThreads.Add(t);
            await _db.SaveChangesAsync();

            return Ok(new { t.Id, t.Subject, t.Status, t.CreatedAt });
        }

        public class CreateThreadReq
        {
            public int? CondominiumId { get; set; }
            public string? Subject { get; set; }
        }

        // Lista threads do utilizador (morador) ou todos (admin)
        [HttpGet("threads")]
        public async Task<IActionResult> ListThreads()
        {
            var userId = User?.Identity?.Name ?? User?.FindFirst("sub")?.Value ?? User?.FindFirst("id")?.Value;
            var isAdmin = User.IsInRole("Administrator") || User.IsInRole("Manager");

            IQueryable<ChatThread> q = _db.ChatThreads.AsNoTracking();
            if (!isAdmin) q = q.Where(t => t.ResidentId == userId);

            var list = await q.OrderByDescending(t => t.LastActivityAt)
                .Select(t => new { t.Id, t.Subject, t.Status, t.CreatedAt, t.LastActivityAt })
                .ToListAsync();

            return Ok(list);
        }

        // Mensagens do thread (paginável por timestamp)
        [HttpGet("threads/{id:int}/messages")]
        public async Task<IActionResult> GetMessages(int id, [FromQuery] DateTime? after)
        {
            var userId = User?.Identity?.Name ?? User?.FindFirst("sub")?.Value ?? User?.FindFirst("id")?.Value;
            var isAdmin = User.IsInRole("Administrator") || User.IsInRole("Manager");

            var th = await _db.ChatThreads.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (th == null) return NotFound();
            if (!isAdmin && th.ResidentId != userId) return Forbid();

            var q = _db.ChatMessages.AsNoTracking().Where(m => m.ThreadId == id);
            if (after.HasValue) q = q.Where(m => m.CreatedAt > after.Value);

            var msgs = await q.OrderBy(m => m.CreatedAt)
                .Select(m => new { m.Id, role = m.Role.ToString(), m.UserId, m.Text, m.CreatedAt })
                .ToListAsync();

            return Ok(msgs);
        }

        // Envia mensagem (REST, útil no MAUI)
        public class SendMessageReq { [Required] public string Text { get; set; } = ""; }

        [HttpPost("threads/{id:int}/messages")]
        public async Task<IActionResult> SendMessage(int id, [FromBody] SendMessageReq req)
        {
            var userId = User?.Identity?.Name ?? User?.FindFirst("sub")?.Value ?? User?.FindFirst("id")?.Value;
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var isAdmin = User.IsInRole("Administrator") || User.IsInRole("Manager");

            var th = await _db.ChatThreads.FirstOrDefaultAsync(t => t.Id == id);
            if (th == null) return NotFound();
            if (!isAdmin && th.ResidentId != userId) return Forbid();
            if (th.Status == "Closed" && !isAdmin) return BadRequest(new { error = "Thread encerrada." });

            var msg = new ChatMessage
            {
                ThreadId = id,
                Role = isAdmin ? ChatRole.Admin : ChatRole.Resident,
                UserId = userId,
                Text = req.Text.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            _db.ChatMessages.Add(msg);
            th.LastActivityAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { msg.Id, role = msg.Role.ToString(), msg.Text, msg.CreatedAt });
        }

        // Admin encerra
        [HttpPost("threads/{id:int}/close")]
        public async Task<IActionResult> CloseThread(int id)
        {
            if (!User.IsInRole("Administrator") && !User.IsInRole("Manager")) return Forbid();

            var th = await _db.ChatThreads.FirstOrDefaultAsync(t => t.Id == id);
            if (th == null) return NotFound();

            th.Status = "Closed";
            await _db.SaveChangesAsync();
            return Ok(new { th.Id, th.Status });
        }
    }
}

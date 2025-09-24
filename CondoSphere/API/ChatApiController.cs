using CondoSphere.API.Models;
using CondoSphere.Data;
using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using CondoSphere.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.API
{
    [Route("api/chat")]
    [ApiController]
    [AllowAnonymous] 
    public class ChatApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IChatBotService _bot;
        private readonly IChatAlertService _chatAlerts;
        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _env;

        public ChatApiController(
            ApplicationDbContext db,
            UserManager<User> userManager,
            IChatBotService bot,
            IChatAlertService chatAlerts,
            IWebHostEnvironment env)
        {
            _db = db; _userManager = userManager; _bot = bot; _chatAlerts = chatAlerts; _env = env;
        }

        private string? MeIdOrNull => _userManager.GetUserId(User);
        private bool IsAdmin => User.IsInRole("Administrator") || User.IsInRole("Manager");

        // ===== THREADS =====
        [HttpGet("threads")]
        [AllowAnonymous]
        public async Task<IActionResult> GetThreads([FromQuery] string? status = null)
        {
            var q = _db.ChatThreads.AsNoTracking().AsQueryable();
            var me = MeIdOrNull;
            if (me != null && !IsAdmin) q = q.Where(t => t.ResidentId == me);
            if (!string.IsNullOrWhiteSpace(status)) q = q.Where(t => t.Status == status);

            var list = await q
                .OrderByDescending(t => t.LastActivityAt)
                .Take(200)
                .Select(t => new {
                    t.Id,
                    t.Subject,
                    t.Status,
                    t.CreatedAt,
                    t.LastActivityAt,
                    UnreadForMe = IsAdmin ? t.UnreadForAdmin : t.UnreadForResident,
                    t.LastPreview,
                    t.HasAttachments
                })
                .ToListAsync();

            return Ok(list);
        }

        [HttpPost("threads")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> CreateThread([FromBody] CreateThreadReq? req)
        {
            // estás sempre logado → nada de “anonymous”
            var residentId = _userManager.GetUserId(User)!;

            // já existe um chat aberto?
            var existing = await _db.ChatThreads
                .Where(t => t.ResidentId == residentId && t.Status == "Open")
                .OrderByDescending(t => t.LastActivityAt)
                .FirstOrDefaultAsync();

            if (existing != null)
                return Conflict(new { message = "Já existe um chat aberto.", threadId = existing.Id });

            // criar novo
            var th = new ChatThread
            {
                ResidentId = residentId,
                CondominiumId = req?.CondominiumId,
                Subject = string.IsNullOrWhiteSpace(req?.Subject) ? "Suporte" : req!.Subject!.Trim(),
                Status = "Open",
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                LastPreview = "Olá! Em que posso ajudar?",
                UnreadForResident = 1
            };
            _db.ChatThreads.Add(th);

            _db.ChatMessages.Add(new ChatMessage
            {
                Thread = th,
                Role = ChatRole.Bot,
                Text = "Olá! Em que posso ajudar?",
                CreatedAt = DateTime.UtcNow,
                IsReadByAdmin = true,
                IsReadByResident = false
            });

            await _db.SaveChangesAsync();

            return Ok(new { th.Id, th.Subject, th.Status, th.CreatedAt, th.LastActivityAt });
        }


        [HttpGet("threads/{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetThread(int id)
        {
            var th = await _db.ChatThreads.FirstOrDefaultAsync(t => t.Id == id);
            if (th == null) return NotFound();

            if (IsAdmin)
            {
                if (th.UnreadForAdmin > 0) th.UnreadForAdmin = 0;
                await _db.ChatMessages.Where(m => m.ThreadId == id && !m.IsReadByAdmin)
                    .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsReadByAdmin, true));
            }
            else
            {
                if (th.UnreadForResident > 0) th.UnreadForResident = 0;
                await _db.ChatMessages.Where(m => m.ThreadId == id && !m.IsReadByResident)
                    .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsReadByResident, true));
            }
            await _db.SaveChangesAsync();

            return Ok(new
            {
                th.Id,
                th.Subject,
                th.Status,
                th.CreatedAt,
                th.LastActivityAt,
                IsAdmin,
                th.CondominiumId
            });
        }

        // ===== MENSAGENS =====
        [HttpGet("threads/{id:int}/messages")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMessages(int id, [FromQuery] int after = 0)
        {
            var th = await _db.ChatThreads.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (th == null) return NotFound();

            var msgs = await _db.ChatMessages.AsNoTracking()
                .Where(m => m.ThreadId == id && m.Id > after)
                .OrderBy(m => m.Id)
                .Select(m => new { m.Id, Role = m.Role.ToString(), m.UserId, m.Text, m.CreatedAt })
                .ToListAsync();

            return Ok(msgs);
        }

        [HttpPost("threads/{id:int}/messages")]
        [AllowAnonymous]
        public async Task<IActionResult> SendMessage(int id, [FromBody] SendMessageReq req)
        {
            if (string.IsNullOrWhiteSpace(req.Text))
                return BadRequest(new { message = "Texto vazio." });

            var th = await _db.ChatThreads.FirstOrDefaultAsync(t => t.Id == id);
            if (th == null) return NotFound();

            var me = MeIdOrNull ?? "anonymous";
            var isAdmin = IsAdmin;

            var msg = new ChatMessage
            {
                ThreadId = id,
                Role = isAdmin ? ChatRole.Admin : ChatRole.Resident,
                UserId = me,
                Text = req.Text.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsReadByAdmin = isAdmin,
                IsReadByResident = !isAdmin
            };
            _db.ChatMessages.Add(msg);

            th.LastActivityAt = msg.CreatedAt;
            th.LastPreview = msg.Text.Length > 120 ? msg.Text[..120] + "…" : msg.Text;
            if (isAdmin) th.UnreadForResident++; else th.UnreadForAdmin++;

            await _db.SaveChangesAsync();

            // bot responde quando “residente” fala
            if (!isAdmin)
            {
                var reply = await _bot.BuildReplyAsync(th, msg);
                if (!string.IsNullOrWhiteSpace(reply))
                {
                    var bot = new ChatMessage
                    {
                        ThreadId = id,
                        Role = ChatRole.Bot,
                        Text = reply,
                        CreatedAt = DateTime.UtcNow,
                        IsReadByAdmin = true,
                        IsReadByResident = false
                    };
                    _db.ChatMessages.Add(bot);

                    th.LastActivityAt = bot.CreatedAt;
                    th.LastPreview = bot.Text.Length > 120 ? bot.Text[..120] + "…" : bot.Text;
                    th.UnreadForResident++;
                    await _db.SaveChangesAsync();
                }
            }

            return Ok(new { msg.Id, Role = msg.Role.ToString(), msg.UserId, msg.Text, msg.CreatedAt });
        }

        [HttpPost("threads/{id:int}/read")]
        [AllowAnonymous]
        public async Task<IActionResult> MarkRead(int id)
        {
            var th = await _db.ChatThreads.FirstOrDefaultAsync(t => t.Id == id);
            if (th == null) return NotFound();

            if (IsAdmin)
            {
                th.UnreadForAdmin = 0;
                await _db.ChatMessages.Where(m => m.ThreadId == id && !m.IsReadByAdmin)
                    .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsReadByAdmin, true));
            }
            else
            {
                th.UnreadForResident = 0;
                await _db.ChatMessages.Where(m => m.ThreadId == id && !m.IsReadByResident)
                    .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsReadByResident, true));
            }
            await _db.SaveChangesAsync();

            return Ok(new { ok = true });
        }

        [HttpPost("threads/{id:int}/attachments")]
        [AllowAnonymous]
        [RequestSizeLimit(30_000_000)]
        public async Task<IActionResult> Upload(int id, IFormFile file, [FromForm] string? text)
        {
            var th = await _db.ChatThreads.FirstOrDefaultAsync(t => t.Id == id);
            if (th == null) return NotFound();
            if (file == null || file.Length == 0) return BadRequest(new { message = "nofile" });

            var me = MeIdOrNull ?? "anonymous";
            var isAdmin = IsAdmin;

            var msg = new ChatMessage
            {
                ThreadId = id,
                Role = isAdmin ? ChatRole.Admin : ChatRole.Resident,
                UserId = me,
                Text = string.IsNullOrWhiteSpace(text) ? "[Anexo]" : text.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsReadByAdmin = isAdmin,
                IsReadByResident = !isAdmin
            };
            _db.ChatMessages.Add(msg);
            await _db.SaveChangesAsync();

            var folder = Path.Combine(_env.WebRootPath, "uploads", "chat", id.ToString());
            Directory.CreateDirectory(folder);
            var ext = Path.GetExtension(file.FileName);
            var safe = Path.GetFileName(file.FileName);
            var phys = Path.Combine(folder, $"{Guid.NewGuid():N}{ext}");
            using (var fs = System.IO.File.Create(phys)) await file.CopyToAsync(fs);

            var rel = $"/uploads/chat/{id}/{Path.GetFileName(phys)}";
            _db.ChatAttachments.Add(new ChatAttachment
            {
                MessageId = msg.Id,
                FileName = safe,
                ContentType = file.ContentType,
                Size = file.Length,
                StoragePath = rel
            });

            th.HasAttachments = true;
            th.LastActivityAt = msg.CreatedAt;
            th.LastPreview = msg.Text.Length > 120 ? msg.Text[..120] + "…" : msg.Text;
            if (isAdmin) th.UnreadForResident++; else th.UnreadForAdmin++;
            await _db.SaveChangesAsync();

            return Ok(new { ok = true, messageId = msg.Id, url = rel, name = safe, when = msg.CreatedAt });
        }
    }
}



using CondoSphere.Data;
using CondoSphere.Hubs;
using CondoSphere.Messaging;
using CondoSphere.Models;
using CondoSphere.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;
        private readonly DomainNotificationService _notify;
        private readonly IHubContext<ChatHub> _hub;

        public ChatController(
            ApplicationDbContext db,
            UserManager<User> userManager,
            DomainNotificationService notify,
            IHubContext<ChatHub> hub)
        {
            _db = db;
            _userManager = userManager;
            _notify = notify;
            _hub = hub;
        }

        // ========== LISTA ==========
        // Admin/Manager: vê todos; Resident: só os próprios
        // Controllers/ChatController.cs -> Index
        [HttpGet]
        public async Task<IActionResult> Index(string? status = null, string? q = null)
        {
            var meId = _userManager.GetUserId(User)!;
            var isAdmin = User.IsInRole("Administrator") || User.IsInRole("Manager");

            var qry = _db.ChatThreads.AsNoTracking()
                .Include(t => t.Messages)
                .OrderByDescending(t => t.LastActivityAt)
                .AsQueryable();

            if (!isAdmin) qry = qry.Where(t => t.ResidentId == meId);
            if (!string.IsNullOrWhiteSpace(status)) qry = qry.Where(t => t.Status == status);
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                qry = qry.Where(t => t.Subject.Contains(q) || t.Messages.Any(m => m.Text.Contains(q)));
            }

            ViewBag.IsAdmin = isAdmin;
            ViewBag.FilterStatus = status;
            ViewBag.Q = q;
            return View(await qry.Take(200).ToListAsync()); // limite
        }


        // ========== CRIAR ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(string? subject = "Suporte", int? condominiumId = null)
        {
            var meId = _userManager.GetUserId(User)!;

            var t = new ChatThread
            {
                ResidentId = meId,
                CondominiumId = condominiumId,
                Subject = string.IsNullOrWhiteSpace(subject) ? "Suporte" : subject.Trim(),
                Status = "Open",
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow
            };

            _db.ChatThreads.Add(t);
            await _db.SaveChangesAsync();

            // Mensagem de boas-vindas do Bot
            var welcome = new ChatMessage
            {
                ThreadId = t.Id,
                Role = ChatRole.Bot,
                UserId = null,
                Text = "Olá! Em que posso ajudar?",
                CreatedAt = DateTime.UtcNow
            };
            _db.ChatMessages.Add(welcome);
            t.LastActivityAt = welcome.CreatedAt;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Thread), new { id = t.Id });
        }

        // ========== DETALHE/CONVERSA ==========
    
        [HttpGet]
        public async Task<IActionResult> Thread(int id)
        {
            var meId = _userManager.GetUserId(User)!;
            var isAdmin = User.IsInRole("Administrator") || User.IsInRole("Manager");

            var t = await _db.ChatThreads
                .Include(x => x.Messages).ThenInclude(m => m.Attachments)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (t == null) return NotFound();
            if (!isAdmin && t.ResidentId != meId) return Forbid();

            // ZERA contadores e marca lidos
            if (isAdmin)
            {
                if (t.UnreadForAdmin > 0) t.UnreadForAdmin = 0;
                foreach (var m in t.Messages.Where(m => !m.IsReadByAdmin))
                    m.IsReadByAdmin = true;
            }
            else
            {
                if (t.UnreadForResident > 0) t.UnreadForResident = 0;
                foreach (var m in t.Messages.Where(m => !m.IsReadByResident))
                    m.IsReadByResident = true;
            }
            await _db.SaveChangesAsync();

            ViewBag.IsAdmin = isAdmin;
            return View(t);
        }


        // ========== ENVIAR MENSAGEM ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int id, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return RedirectToAction(nameof(Thread), new { id });

            var meId = _userManager.GetUserId(User)!;
            var isAdmin = User.IsInRole("Administrator") || User.IsInRole("Manager");

            var t = await _db.ChatThreads.FirstOrDefaultAsync(x => x.Id == id);
            if (t == null) return NotFound();
            if (!isAdmin && t.ResidentId != meId) return Forbid();
            if (!string.Equals(t.Status, "Open", StringComparison.OrdinalIgnoreCase))
            {
                TempData["chat_err"] = "Este tópico está fechado.";
                return RedirectToAction(nameof(Thread), new { id });
            }

            var role = isAdmin ? ChatRole.Admin : ChatRole.Resident;

            var msg = new ChatMessage
            {
                ThreadId = t.Id,
                Role = role,
                UserId = meId,
                Text = text.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            _db.ChatMessages.Add(msg);
            t.LastActivityAt = msg.CreatedAt;
            await _db.SaveChangesAsync();

            // Bot responde se quem falou foi o residente
            if (role == ChatRole.Resident)
            {
                var botReply = await BotReplyAsync(t, msg);
                if (!string.IsNullOrWhiteSpace(botReply))
                {
                    var botMsg = new ChatMessage
                    {
                        ThreadId = t.Id,
                        Role = ChatRole.Bot,
                        UserId = null,
                        Text = botReply,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.ChatMessages.Add(botMsg);
                    t.LastActivityAt = botMsg.CreatedAt;
                    await _db.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Thread), new { id });
        }

        [HttpPost, Authorize(Roles = "Administrator,Manager"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int id)
        {
            var t = await _db.ChatThreads.FindAsync(id);
            if (t == null) return NotFound();
            t.Status = "Closed";
            await _db.SaveChangesAsync();
            await _hub.Clients.Group($"thread-{id}").SendAsync("ThreadClosed");
            return RedirectToAction(nameof(Thread), new { id });
        }

        [HttpPost, Authorize(Roles = "Administrator,Manager"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Reopen(int id)
        {
            var t = await _db.ChatThreads.FindAsync(id);
            if (t == null) return NotFound();
            t.Status = "Open";
            t.LastActivityAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await _hub.Clients.Group($"thread-{id}").SendAsync("ThreadReopened");
            return RedirectToAction(nameof(Thread), new { id });
        }


        [HttpPost, Authorize(Roles = "Administrator,Manager"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteThread(int id)
        {
            var t = await _db.ChatThreads
                .Include(x => x.Messages).ThenInclude(m => m.Attachments)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (t == null) return NotFound();

            foreach (var a in t.Messages.SelectMany(m => m.Attachments ?? new List<ChatAttachment>()))
            {
                var full = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                    a.StoragePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
            }

            _db.ChatThreads.Remove(t);
            await _db.SaveChangesAsync();

            await _hub.Clients.Group($"thread-{id}").SendAsync("ThreadClosed");
            await _hub.Clients.Group("admins").SendAsync("ThreadUpdated", new { id, deleted = true });

            return RedirectToAction(nameof(Index));
        }

        [HttpPost, Authorize(Roles = "Administrator,Manager"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var m = await _db.ChatMessages.Include(x => x.Attachments).FirstOrDefaultAsync(x => x.Id == id);
            if (m == null) return NotFound();

            foreach (var a in m.Attachments ?? Enumerable.Empty<ChatAttachment>())
            {
                var full = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                    a.StoragePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
            }

            _db.ChatMessages.Remove(m);
            await _db.SaveChangesAsync();
            return Ok(new { ok = true });
        }



        // ========== BOT (simples, palavras-chave) ==========
        private async Task<string?> BotReplyAsync(ChatThread thread, ChatMessage lastUserMessage)
        {
            var txt = (lastUserMessage.Text ?? "").ToLowerInvariant();

            if (txt.Contains("elevador") || txt.Contains("avaria") || txt.Contains("manuten"))
            {
                await _notify.MaintenanceRequestReceivedAsync(
                    to: "support@condosphere-web-app.somee.com",
                    title: "Avaria reportada via chat",
                    requestId: thread.Id,
                    condoName: thread.CondominiumId?.ToString() ?? "—");

                return "Obrigado. Registámos a sua avaria e a administração foi notificada. Entraremos em contacto.";
            }

            if (txt.Contains("barulho") || txt.Contains("ruido") || txt.Contains("ruído"))
                return "Obrigado pelo aviso. Vamos avaliar a situação e orientar conforme o regulamento interno.";

            if (txt.Contains("quota") || txt.Contains("pagamento") || txt.Contains("mensalidade"))
                return "Para pagar a quota: menu Quotas → Pay. Se precisar, peça aqui que um gestor envia o link.";

            if (txt.Contains("documento") || txt.Contains("ata") || txt.Contains("regulamento"))
                return "Documentos/atas: a administração pode enviar sob pedido. Encaminhei sua solicitação.";

            return null;
        }


        // ===== Enviar mensagem via API (polling) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendApi(int id, string text)
        {
            text = (text ?? "").Trim();
            if (string.IsNullOrEmpty(text)) return BadRequest();

            var meId = _userManager.GetUserId(User)!;
            var isAdmin = User.IsInRole("Administrator") || User.IsInRole("Manager");

            var t = await _db.ChatThreads.FirstOrDefaultAsync(x => x.Id == id);
            if (t == null) return NotFound();
            if (!isAdmin && t.ResidentId != meId) return Forbid();

            // ✅ bloqueia envio para todos quando fechado
            if (t.Status == "Closed") return BadRequest("closed");

            var msg = new ChatMessage
            {
                ThreadId = t.Id,
                Role = isAdmin ? ChatRole.Admin : ChatRole.Resident,
                UserId = meId,
                Text = text,
                CreatedAt = DateTime.UtcNow,
                IsReadByAdmin = isAdmin,
                IsReadByResident = !isAdmin
            };
            _db.ChatMessages.Add(msg);

            // preview + contadores + last activity
            t.LastActivityAt = msg.CreatedAt;
            t.LastPreview = text.Length > 120 ? text[..120] + "…" : text;
            if (isAdmin) t.UnreadForResident++; else t.UnreadForAdmin++;

            await _db.SaveChangesAsync();

            // bot só responde quando quem falou foi o residente
            if (!isAdmin)
            {
                var bot = await BotReplyAsync(t, msg);
                if (!string.IsNullOrWhiteSpace(bot))
                {
                    var botMsg = new ChatMessage
                    {
                        ThreadId = t.Id,
                        Role = ChatRole.Bot,
                        UserId = null,
                        Text = bot,
                        CreatedAt = DateTime.UtcNow,
                        IsReadByAdmin = true,
                        IsReadByResident = false
                    };
                    _db.ChatMessages.Add(botMsg);

                    t.LastActivityAt = botMsg.CreatedAt;
                    t.LastPreview = bot.Length > 120 ? bot[..120] + "…" : bot;
                    t.UnreadForResident++;

                    await _db.SaveChangesAsync();
                }
            }

            return Json(new { ok = true, id = msg.Id, when = msg.CreatedAt });
        }

        // Retorna mensagens novas após um id
        [HttpGet]
        public async Task<IActionResult> Poll(int id, long after = 0)
        {
            var t = await _db.ChatThreads
                .Include(x => x.Messages)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (t == null) return NotFound();

            var items = t.Messages
                .Where(m => m.Id > after)
                .OrderBy(m => m.Id)
                .Select(m => new { m.Id, role = m.Role.ToString(), m.Text, createdAt = m.CreatedAt })
                .ToList();

            return Json(items);
        }

        [HttpPost]
        [Authorize(Roles = "Resident")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartApi(string subject = "Suporte")
        {
            var userId = _userManager.GetUserId(User)!;

            // Reaproveita uma thread "Open" do usuário p/ não criar n duplicadas
            var th = await _db.ChatThreads
                .Where(t => t.ResidentId == userId && t.Status == "Open")
                .OrderByDescending(t => t.LastActivityAt)
                .FirstOrDefaultAsync();

            if (th == null)
            {
                th = new ChatThread
                {
                    ResidentId = userId,
                    Subject = subject,
                    Status = "Open"
                };
                _db.ChatThreads.Add(th);

                // saudação do bot
                _db.ChatMessages.Add(new ChatMessage
                {
                    Thread = th,
                    Role = ChatRole.Bot,
                    Text = "Olá! Em que posso ajudar?"
                });

                await _db.SaveChangesAsync();
            }

            return Json(new { ok = true, id = th.Id });
        }
        // ===== Lista incremental para admins (polling do Index) =====
        [HttpGet, Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> AdminPoll(long? afterTicks = null)
        {
            var since = afterTicks.HasValue
                ? new DateTime(afterTicks.Value, DateTimeKind.Utc)
                : DateTime.UtcNow.AddMinutes(-60);

            var items = await _db.ChatThreads.AsNoTracking()
                .Where(t => t.LastActivityAt >= since)
                .OrderByDescending(t => t.LastActivityAt)
                .Select(t => new {
                    id = t.Id,
                    subject = t.Subject,
                    status = t.Status,
                    last = t.LastPreview,
                    lastAt = t.LastActivityAt,
                    unread = t.UnreadForAdmin
                })
                .ToListAsync();

            return Json(new { now = DateTime.UtcNow.Ticks, items });
        }


        // Helpers
        private static bool IsAjax(HttpRequest req) =>
            string.Equals(req.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase) ||
            req.Headers["Accept"].ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);

        // ===== Upload via widget (somente Resident) =====
        // ===== Upload via widget (somente Resident) =====
        [HttpPost, Authorize(Roles = "Resident")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int id, IFormFile file, string? text)
        {
            // este upload é chamado pelo widget do morador,
            // admin/manager NÃO têm esse formulário na tela de thread
            if (file == null || file.Length == 0)
                return BadRequest(new { ok = false, error = "nofile" });

            var meId = _userManager.GetUserId(User)!;

            var t = await _db.ChatThreads.FirstOrDefaultAsync(x => x.Id == id);
            if (t == null) return NotFound();
            if (t.ResidentId != meId) return Forbid();
            if (t.Status == "Closed") return BadRequest("closed");

            var msg = new ChatMessage
            {
                ThreadId = id,
                Role = ChatRole.Resident,
                UserId = meId,
                Text = string.IsNullOrWhiteSpace(text) ? "[Anexo]" : text.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsReadByAdmin = false,
                IsReadByResident = true
            };
            _db.ChatMessages.Add(msg);
            await _db.SaveChangesAsync();

            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "chat", id.ToString());
            Directory.CreateDirectory(folder);
            var ext = Path.GetExtension(file.FileName);
            var safeName = Path.GetFileName(file.FileName);
            var phys = Path.Combine(folder, $"{Guid.NewGuid():N}{ext}");
            await using (var fs = System.IO.File.Create(phys))
                await file.CopyToAsync(fs);

            var rel = $"/uploads/chat/{id}/{Path.GetFileName(phys)}";
            _db.ChatAttachments.Add(new ChatAttachment
            {
                MessageId = msg.Id,
                FileName = safeName,
                ContentType = file.ContentType,
                Size = file.Length,
                StoragePath = rel
            });

            t.HasAttachments = true;
            t.LastActivityAt = msg.CreatedAt;
            t.LastPreview = msg.Text.Length > 120 ? msg.Text[..120] + "…" : msg.Text;
            t.UnreadForAdmin++;
            await _db.SaveChangesAsync();

            return Json(new { ok = true, id = msg.Id, url = rel, name = safeName, when = msg.CreatedAt });
        }


    }
}

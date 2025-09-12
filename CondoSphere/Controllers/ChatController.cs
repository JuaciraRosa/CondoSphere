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

        // ========== FECHAR / REABRIR ==========
        [HttpPost]
        [Authorize(Roles = "Administrator,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int id)
        {
            var t = await _db.ChatThreads.FindAsync(id);
            if (t == null) return NotFound();
            t.Status = "Closed";
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Thread), new { id });
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reopen(int id)
        {
            var t = await _db.ChatThreads.FindAsync(id);
            if (t == null) return NotFound();
            t.Status = "Open";
            t.LastActivityAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Thread), new { id });
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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int id, IFormFile file, string? text)
        {
            if (file == null || file.Length == 0)
            {
                TempData["chat_err"] = "Selecione um ficheiro.";
                return RedirectToAction(nameof(Thread), new { id });
            }

            var meId = _userManager.GetUserId(User)!;
            var isAdmin = User.IsInRole("Administrator") || User.IsInRole("Manager");
            var t = await _db.ChatThreads.FirstOrDefaultAsync(x => x.Id == id);
            if (t == null) return NotFound();
            if (!isAdmin && t.ResidentId != meId) return Forbid();

            var role = isAdmin ? ChatRole.Admin : ChatRole.Resident;

            // cria mensagem (texto opcional)
            var msg = new ChatMessage
            {
                ThreadId = id,
                Role = role,
                UserId = meId,
                Text = string.IsNullOrWhiteSpace(text) ? "[Anexo]" : text.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsReadByAdmin = isAdmin,
                IsReadByResident = !isAdmin
            };
            _db.ChatMessages.Add(msg);
            await _db.SaveChangesAsync();

            // salva ficheiro
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

            // counters + preview
            t.HasAttachments = true;
            t.LastActivityAt = msg.CreatedAt;
            t.LastPreview = msg.Text.Length > 120 ? msg.Text[..120] + "…" : msg.Text;
            if (isAdmin) t.UnreadForResident++;
            else t.UnreadForAdmin++;
            await _db.SaveChangesAsync();

            // notifica thread e inbox admin
            await _hub.Clients.Group($"thread-{id}").SendAsync("ReceiveMessage", new
            {
                id = msg.Id,
                role = msg.Role.ToString(),
                userId = msg.UserId,
                text = msg.Text,
                attachmentUrl = rel,
                attachmentName = safeName,
                createdAt = msg.CreatedAt
            });
            await _hub.Clients.Group("admins").SendAsync("ThreadUpdated", new
            {
                id = t.Id,
                subject = t.Subject,
                status = t.Status,
                last = t.LastPreview,
                lastAt = t.LastActivityAt,
                unread = t.UnreadForAdmin
            });

            return RedirectToAction(nameof(Thread), new { id });
        }

    }
}

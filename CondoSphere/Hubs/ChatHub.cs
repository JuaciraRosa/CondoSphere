using CondoSphere.Data;
using CondoSphere.Models;
using CondoSphere.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace CondoSphere.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _db;
        private readonly IChatBotService _bot;

        public ChatHub(ApplicationDbContext db, IChatBotService bot)
        {
            _db = db;
            _bot = bot;
        }

        private static string GroupName(int threadId) => $"thread-{threadId}";
        private static string Preview(string text) =>
            string.IsNullOrWhiteSpace(text) ? "" : (text.Length <= 120 ? text : text[..120] + "…");

        public async Task JoinThread(int threadId)
        {
            var userId = Context.UserIdentifier!;
            var isAdmin = Context.User.IsInRole("Administrator") || Context.User.IsInRole("Manager");
            if (isAdmin) await Groups.AddToGroupAsync(Context.ConnectionId, "admins");

            var th = await _db.ChatThreads.AsNoTracking().FirstOrDefaultAsync(t => t.Id == threadId)
                     ?? throw new HubException("Thread inexistente.");

            if (!isAdmin && th.ResidentId != userId)
                throw new HubException("Sem acesso a este chat.");

            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(threadId)); // (único)
        }

        public async Task SendMessage(int threadId, string text)
        {
            text = (text ?? "").Trim();
            if (string.IsNullOrEmpty(text)) return;

            var userId = Context.UserIdentifier!;
            var isAdmin = Context.User.IsInRole("Administrator") || Context.User.IsInRole("Manager");

            var th = await _db.ChatThreads.FirstOrDefaultAsync(t => t.Id == threadId)
                     ?? throw new HubException("Thread inexistente.");

            if (!isAdmin && th.ResidentId != userId)
                throw new HubException("Sem acesso a este chat.");

            if (th.Status == "Closed" && !isAdmin)
                throw new HubException("Thread encerrada.");

            var msg = new ChatMessage
            {
                ThreadId = threadId,
                Role = isAdmin ? ChatRole.Admin : ChatRole.Resident,
                UserId = userId,
                Text = text,
                CreatedAt = DateTime.UtcNow,
                IsReadByAdmin = isAdmin,
                IsReadByResident = !isAdmin
            };
            _db.ChatMessages.Add(msg);

            // Atualiza preview, contadores e last activity
            th.LastActivityAt = msg.CreatedAt;
            th.LastPreview = Preview(text);
            if (isAdmin) th.UnreadForResident++;
            else th.UnreadForAdmin++;

            await _db.SaveChangesAsync();

            await Clients.Group(GroupName(threadId)).SendAsync("ReceiveMessage", new
            {
                id = msg.Id,
                role = msg.Role.ToString(),
                userId = msg.UserId,
                text = msg.Text,
                createdAt = msg.CreatedAt
            });

            // Atualiza inbox da administração SEMPRE que chega mensagem nova
            await Clients.Group("admins").SendAsync("ThreadUpdated", new
            {
                id = th.Id,
                subject = th.Subject,
                status = th.Status,
                last = th.LastPreview,
                lastAt = th.LastActivityAt,
                unread = th.UnreadForAdmin
            });

            // Se foi o morador, deixa o bot responder
            if (msg.Role == ChatRole.Resident)
            {
                var replyText = await _bot.BuildReplyAsync(th, msg);
                if (!string.IsNullOrWhiteSpace(replyText))
                {
                    var botMsg = new ChatMessage
                    {
                        ThreadId = threadId,
                        Role = ChatRole.Bot,
                        UserId = null,
                        Text = replyText,
                        CreatedAt = DateTime.UtcNow,
                        IsReadByAdmin = true,
                        IsReadByResident = false
                    };
                    _db.ChatMessages.Add(botMsg);

                    th.LastActivityAt = botMsg.CreatedAt;
                    th.LastPreview = Preview(botMsg.Text);
                    th.UnreadForResident++;

                    await _db.SaveChangesAsync();

                    await Clients.Group(GroupName(threadId)).SendAsync("ReceiveMessage", new
                    {
                        id = botMsg.Id,
                        role = botMsg.Role.ToString(),
                        userId = (string?)null,
                        text = botMsg.Text,
                        createdAt = botMsg.CreatedAt
                    });

                    await Clients.Group("admins").SendAsync("ThreadUpdated", new
                    {
                        id = th.Id,
                        subject = th.Subject,
                        status = th.Status,
                        last = th.LastPreview,
                        lastAt = th.LastActivityAt,
                        unread = th.UnreadForAdmin
                    });
                }
            }
        }
        public override async Task OnConnectedAsync()
        {
            var isAdmin = Context.User.IsInRole("Administrator") || Context.User.IsInRole("Manager");
            if (isAdmin)
                await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
            await base.OnConnectedAsync();
        }

        public async Task CloseThread(int threadId)
        {
            var isAdmin = Context.User.IsInRole("Administrator") || Context.User.IsInRole("Manager");
            if (!isAdmin) throw new HubException("Apenas administração.");

            var th = await _db.ChatThreads.FirstOrDefaultAsync(t => t.Id == threadId)
                     ?? throw new HubException("Thread inexistente.");

            th.Status = "Closed";
            await _db.SaveChangesAsync();

            await Clients.Group(GroupName(threadId)).SendAsync("ThreadClosed", new { threadId });
            await Clients.Group("admins").SendAsync("ThreadUpdated", new
            {
                id = threadId,
                status = "Closed",
                last = th.LastPreview,
                lastAt = th.LastActivityAt,
                unread = th.UnreadForAdmin
            });
        }
    }
}

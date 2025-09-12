using CondoSphere.Models;

namespace CondoSphere.Services
{
    public interface IChatBotService
    {
        Task<string?> BuildReplyAsync(ChatThread thread, ChatMessage lastUserMessage);
    }
}

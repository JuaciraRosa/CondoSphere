using CondoSphere.Models;

namespace CondoSphere.Data.Interfaces
{
    public interface IForumRepository
    {
        // Categories
        Task<List<ForumCategory>> GetCategoriesAsync();
        Task<ForumCategory?> GetCategoryAsync(int id);

        // Topics
        Task<List<ForumTopic>> GetTopicsByCategoryAsync(int categoryId, int page, int pageSize, int? companyId = null, int? condominiumId = null);
        Task<int> CountTopicsByCategoryAsync(
      int categoryId, int? companyId = null, int? condominiumId = null);
        Task<ForumTopic?> GetTopicAsync(int id);
        Task<int> CreateTopicAsync(ForumTopic topic, ForumPost firstPost);
        Task TogglePinAsync(int topicId, bool pin);
        Task ToggleLockAsync(int topicId, bool locked);
        Task IncrementViewsAsync(int topicId);

        // Posts
        Task<List<ForumPost>> GetPostsAsync(int topicId, int page, int pageSize);
        Task<int> CountPostsAsync(int topicId);
        Task<int> AddReplyAsync(ForumPost post);
        Task SoftDeletePostAsync(int postId, string deletedById);

        // Subscriptions
        Task<bool> IsSubscribedAsync(int topicId, string userId);
        Task SubscribeAsync(int topicId, string userId);
        Task UnsubscribeAsync(int topicId, string userId);
        Task<List<string>> GetSubscriberEmailsAsync(int topicId);


        Task<int> AddPostAsync(ForumPost post);                 // alias para AddReplyAsync
        Task AddAttachmentsAsync(IEnumerable<ForumAttachment> attachments);
        Task<ForumAttachment?> GetAttachmentAsync(int id);
        Task<ForumPost?> GetPostAsync(int postId);
        Task RestorePostAsync(int postId);
        Task TouchTopicAsync(int topicId);




        // Reactions (exclusivo: 1 reação por utilizador por post)
        Task ToggleReactionExclusiveAsync(int postId, string userId, string emoji);

        // Para render inicial (página do tópico)
        Task<Dictionary<int, Dictionary<string, int>>> CountReactionsForTopicAsync(int topicId);
        Task<Dictionary<int, HashSet<string>>> GetUserReactionsForTopicAsync(int topicId, string userId);

        // Para reconciliação via AJAX depois do clique
        Task<Dictionary<string, int>> CountReactionsForPostAsync(int postId);
        Task<HashSet<string>> GetUserReactionsForPostAsync(int postId, string userId);

        Task<int> CreateCategoryAsync(ForumCategory c);




    }
}

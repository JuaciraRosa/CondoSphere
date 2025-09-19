using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Data.Repositories
{
    public class ForumRepository : IForumRepository
    {
        private readonly ApplicationDbContext _ctx;
        public ForumRepository(ApplicationDbContext ctx) => _ctx = ctx;

        // Categories
        public Task<List<ForumCategory>> GetCategoriesAsync() =>
            _ctx.ForumCategories.AsNoTracking()
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .ToListAsync();

        public Task<ForumCategory?> GetCategoryAsync(int id) =>
            _ctx.ForumCategories.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

        // Topics
        public async Task<List<ForumTopic>> GetTopicsByCategoryAsync(
       int categoryId, int page, int pageSize,
       int? companyId = null, int? condominiumId = null)
        {
            var q = _ctx.ForumTopics.AsNoTracking()
                .Where(t => t.CategoryId == categoryId);

            if (companyId.HasValue) q = q.Where(t => t.CompanyId == companyId.Value);
            if (condominiumId.HasValue) q = q.Where(t => t.CondominiumId == condominiumId.Value);

            return await q.OrderByDescending(t => t.IsPinned)
                          .ThenByDescending(t => t.LastPostAtUtc)
                          .Skip((page - 1) * pageSize)
                          .Take(pageSize)
                          .ToListAsync();
        }

        public Task<int> CountTopicsByCategoryAsync(
            int categoryId, int? companyId = null, int? condominiumId = null)
        {
            var q = _ctx.ForumTopics.AsNoTracking()
                .Where(t => t.CategoryId == categoryId);

            if (companyId.HasValue) q = q.Where(t => t.CompanyId == companyId.Value);
            if (condominiumId.HasValue) q = q.Where(t => t.CondominiumId == condominiumId.Value);

            return q.CountAsync();
        }

        public Task<ForumTopic?> GetTopicAsync(int id) =>
            _ctx.ForumTopics.AsNoTracking()
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == id);

        public async Task<int> CreateTopicAsync(ForumTopic topic, ForumPost firstPost)
        {
            // Garante que a transação funciona com a strategy de retry do SQL Server
            var strategy = _ctx.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _ctx.Database.BeginTransactionAsync();

                topic.LastPostAtUtc = DateTime.UtcNow;
                _ctx.ForumTopics.Add(topic);
                await _ctx.SaveChangesAsync();           // gera topic.Id

                firstPost.TopicId = topic.Id;
                _ctx.ForumPosts.Add(firstPost);
                await _ctx.SaveChangesAsync();

                await tx.CommitAsync();
                return topic.Id;
            });
        }


        public async Task TogglePinAsync(int topicId, bool pin)
        {
            var t = await _ctx.ForumTopics.FirstOrDefaultAsync(x => x.Id == topicId);
            if (t == null) return;
            t.IsPinned = pin;
            await _ctx.SaveChangesAsync();
        }

        public async Task ToggleLockAsync(int topicId, bool locked)
        {
            var t = await _ctx.ForumTopics.FirstOrDefaultAsync(x => x.Id == topicId);
            if (t == null) return;
            t.IsLocked = locked;
            await _ctx.SaveChangesAsync();
        }

        public async Task IncrementViewsAsync(int topicId)
        {
            var t = await _ctx.ForumTopics.FirstOrDefaultAsync(x => x.Id == topicId);
            if (t == null) return;
            t.ViewsCount++;
            await _ctx.SaveChangesAsync();
        }

        // Posts
        public async Task<List<ForumPost>> GetPostsAsync(int topicId, int page, int pageSize)
        {
            var q = _ctx.ForumPosts.AsNoTracking()
                .Where(p => p.TopicId == topicId)
                .Include(p => p.Attachments) 
                .OrderBy(p => p.CreatedAtUtc);

            return await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        }

        public Task<int> CountPostsAsync(int topicId) =>
            _ctx.ForumPosts.AsNoTracking().CountAsync(p => p.TopicId == topicId);

        public async Task<int> AddReplyAsync(ForumPost post)
        {
            _ctx.ForumPosts.Add(post);

            var topic = await _ctx.ForumTopics.FirstOrDefaultAsync(t => t.Id == post.TopicId);
            if (topic != null)
            {
                topic.LastPostAtUtc = DateTime.UtcNow;
            }

            await _ctx.SaveChangesAsync();
            return post.Id;
        }

        public async Task SoftDeletePostAsync(int postId, string deletedById)
        {
            var post = await _ctx.ForumPosts.FirstOrDefaultAsync(p => p.Id == postId);
            if (post == null) return;
            post.IsDeleted = true;
            post.DeletedById = deletedById;
            post.EditedAtUtc = DateTime.UtcNow;
            await _ctx.SaveChangesAsync();
        }

        // Subscriptions
        public Task<bool> IsSubscribedAsync(int topicId, string userId) =>
            _ctx.ForumSubscriptions.AsNoTracking()
                .AnyAsync(s => s.TopicId == topicId && s.UserId == userId);

        public async Task SubscribeAsync(int topicId, string userId)
        {
            if (!await IsSubscribedAsync(topicId, userId))
            {
                _ctx.ForumSubscriptions.Add(new ForumSubscription
                {
                    TopicId = topicId,
                    UserId = userId
                });
                await _ctx.SaveChangesAsync();
            }
        }

        public async Task UnsubscribeAsync(int topicId, string userId)
        {
            var s = await _ctx.ForumSubscriptions
                .FirstOrDefaultAsync(x => x.TopicId == topicId && x.UserId == userId);
            if (s != null)
            {
                _ctx.ForumSubscriptions.Remove(s);
                await _ctx.SaveChangesAsync();
            }
        }

        public async Task<List<string>> GetSubscriberEmailsAsync(int topicId)
        {
            var q = from s in _ctx.ForumSubscriptions
                    join u in _ctx.Users on s.UserId equals u.Id
                    where s.TopicId == topicId && u.IsActive && u.Email != null
                    select u.Email!;
            return await q.Distinct().ToListAsync();
        }


        public Task<int> AddPostAsync(ForumPost post) => AddReplyAsync(post);

        public async Task AddAttachmentsAsync(IEnumerable<ForumAttachment> attachments)
        {
            if (attachments == null) return;
            _ctx.ForumAttachments.AddRange(attachments);
            await _ctx.SaveChangesAsync();
        }

        public Task<ForumAttachment?> GetAttachmentAsync(int id) =>
            _ctx.ForumAttachments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);

        public Task<ForumPost?> GetPostAsync(int postId) =>
            _ctx.ForumPosts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == postId);

        public async Task RestorePostAsync(int postId)
        {
            var post = await _ctx.ForumPosts.FirstOrDefaultAsync(p => p.Id == postId);
            if (post == null) return;
            post.IsDeleted = false;
            post.DeleteReason = null;
            post.DeletedById = null;
            post.EditedAtUtc = DateTime.UtcNow;
            await _ctx.SaveChangesAsync();
        }

        public async Task TouchTopicAsync(int topicId)
        {
            var t = await _ctx.ForumTopics.FirstOrDefaultAsync(x => x.Id == topicId);
            if (t == null) return;
            t.LastPostAtUtc = DateTime.UtcNow;
            await _ctx.SaveChangesAsync();
        }



        // ===== Reactions (EXCLUSIVE) =====
        public async Task ToggleReactionExclusiveAsync(int postId, string userId, string emoji)
        {
            emoji = (emoji ?? "").Trim();
            if (string.IsNullOrWhiteSpace(emoji)) return;

            // todas as reações deste utilizador neste post
            var mine = await _ctx.ForumReactions
                .Where(r => r.PostId == postId && r.UserId == userId)
                .ToListAsync();

            var hasSame = mine.Any(r => r.Emoji == emoji);

            if (hasSame)
            {
                // clicou novamente na mesma -> remover (toggle off)
                _ctx.ForumReactions.RemoveRange(mine.Where(r => r.Emoji == emoji));
            }
            else
            {
                // mudar: remover outras e adicionar a nova
                _ctx.ForumReactions.RemoveRange(mine);
                _ctx.ForumReactions.Add(new ForumReaction { PostId = postId, UserId = userId, Emoji = emoji });
            }

            await _ctx.SaveChangesAsync();
        }

        // contagens por POST (para reconciliação após clique)
        public async Task<Dictionary<string, int>> CountReactionsForPostAsync(int postId)
        {
            var rows = await _ctx.ForumReactions.AsNoTracking()
                .Where(r => r.PostId == postId)
                .GroupBy(r => r.Emoji)
                .Select(g => new { Emoji = g.Key!, Cnt = g.Count() })
                .ToListAsync();

            return rows.ToDictionary(x => x.Emoji, x => x.Cnt);
        }

        // quais emojis deste POST são do utilizador
        public async Task<HashSet<string>> GetUserReactionsForPostAsync(int postId, string userId)
        {
            var list = await _ctx.ForumReactions.AsNoTracking()
                .Where(r => r.PostId == postId && r.UserId == userId)
                .Select(r => r.Emoji!)
                .ToListAsync();

            return new HashSet<string>(list);
        }

        // contagens para TODOS os posts do tópico (render inicial)
        public async Task<Dictionary<int, Dictionary<string, int>>> CountReactionsForTopicAsync(int topicId)
        {
            var postIds = await _ctx.ForumPosts.AsNoTracking()
                .Where(p => p.TopicId == topicId)
                .Select(p => p.Id)
                .ToListAsync();

            var rows = await _ctx.ForumReactions.AsNoTracking()
                .Where(r => postIds.Contains(r.PostId))
                .GroupBy(r => new { r.PostId, r.Emoji })
                .Select(g => new { g.Key.PostId, g.Key.Emoji, Cnt = g.Count() })
                .ToListAsync();

            var map = new Dictionary<int, Dictionary<string, int>>();
            foreach (var r in rows)
            {
                if (!map.TryGetValue(r.PostId, out var inner))
                    inner = map[r.PostId] = new Dictionary<string, int>();
                inner[r.Emoji] = r.Cnt;
            }
            return map;
        }

        // quais emojis (por post) pertencem ao utilizador no tópico (render inicial)
        public async Task<Dictionary<int, HashSet<string>>> GetUserReactionsForTopicAsync(int topicId, string userId)
        {
            var postIds = await _ctx.ForumPosts.AsNoTracking()
                .Where(p => p.TopicId == topicId)
                .Select(p => p.Id)
                .ToListAsync();

            var recs = await _ctx.ForumReactions.AsNoTracking()
                .Where(r => r.UserId == userId && postIds.Contains(r.PostId))
                .Select(r => new { r.PostId, r.Emoji })
                .ToListAsync();

            var map = new Dictionary<int, HashSet<string>>();
            foreach (var r in recs)
            {
                if (!map.TryGetValue(r.PostId, out var set))
                    set = map[r.PostId] = new HashSet<string>();
                set.Add(r.Emoji!);
            }
            return map;
        }
      
        public async Task<int> CreateCategoryAsync(ForumCategory c)
        {
            _ctx.ForumCategories.Add(c);
            await _ctx.SaveChangesAsync();
            return c.Id;
        }



    }
}

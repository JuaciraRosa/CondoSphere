using CondoSphere.Data.Interfaces;
using CondoSphere.Messaging;
using CondoSphere.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace CondoSphere.Controllers
{
    [Authorize] // everyone authenticated can read/post; locks are enforced inside actions
    public class ForumController : Controller
    {
        private readonly IForumRepository _repo;
        private readonly DomainNotificationService _notify;
        private readonly IWebHostEnvironment _env;
        private readonly ICondominiumRepository? _condos;

        public ForumController(IForumRepository repo, DomainNotificationService notify, IWebHostEnvironment env, ICondominiumRepository? condos = null)
        {
            _repo = repo;
            _notify = notify;
            _env = env;
            _condos = condos;
        }

        private string? UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
        private string? UserEmail() => User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;

        // GET: /Forum
        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var cats = await _repo.GetCategoriesAsync();
            return View(cats);
        }

        // GET: /Forum/Category/5?page=1
        [HttpGet]
        public async Task<IActionResult> Category(int id, int page = 1)
        {
            const int pageSize = 20;

            // Escopo padrão: empresa do user (condomínio opcional)
            int? companyId = null;
            if (int.TryParse(User.FindFirst("companyId")?.Value, out var cid))
                companyId = cid;

            int? condoId = null; // deixe null se quer “todos os condomínios”

            var topics = await _repo.GetTopicsByCategoryAsync(id, page, pageSize, companyId, condoId);
            var total = await _repo.CountTopicsByCategoryAsync(id, companyId, condoId);

            ViewBag.Category = await _repo.GetCategoryAsync(id);
            ViewBag.Page = page;
            ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));

            return View(topics);
        }


        // GET: /Forum/Topic/10?page=1
        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> Topic(int id, int page = 1, int pageSize = 20)
        {
            page = Math.Max(1, page);

            var topic = await _repo.GetTopicAsync(id);
            if (topic == null) return NotFound();

            await _repo.IncrementViewsAsync(id);

            var posts = await _repo.GetPostsAsync(id, page, pageSize);
            var total = await _repo.CountPostsAsync(id);

            ViewBag.Topic = topic;
            ViewBag.Page = page;
            ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            ViewBag.CategoryId = topic.CategoryId;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.IsSubscribed = !string.IsNullOrEmpty(userId) && await _repo.IsSubscribedAsync(id, userId!);

            // Reactions
            ViewBag.ReactionCounts = await _repo.CountReactionsForTopicAsync(id); // Dict<int postId, Dict<string emoji, int count>>
            ViewBag.UserReactions = string.IsNullOrEmpty(userId)
                ? new Dictionary<int, HashSet<string>>()
                : await _repo.GetUserReactionsForTopicAsync(id, userId!);

            // Para “Back to category”
            ViewBag.CategoryId = topic.CategoryId;

            ViewBag.Attachments = posts
            .Where(p => p.Attachments != null && p.Attachments.Count > 0)
             .ToDictionary(
              p => p.Id,
              p => (IEnumerable<ForumAttachment>)p.Attachments
             );

            return View(posts);
        }



        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> React(int topicId, int postId, string emoji, int page = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(emoji))
                return BadRequest();

            await _repo.ToggleReactionExclusiveAsync(postId, userId, emoji);

            // Se for AJAX, devolve os contadores atualizados desse post + quais emojis eu reagi
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var counts = await _repo.CountReactionsForPostAsync(postId);
                var mine = await _repo.GetUserReactionsForPostAsync(postId, userId);
                return Json(new { ok = true, counts, mine = mine.ToArray() });
            }

            // fallback: navegação normal
            var url = Url.Action(nameof(Topic), new { id = topicId, page = Math.Max(1, page) }) ?? $"/Forum/Topic/{topicId}";
            return Redirect(url + $"#p{postId}");
        }




        // GET: /Forum/NewTopic/5
        [HttpGet]
        public async Task<IActionResult> NewTopic(int categoryId)
        {
            var category = await _repo.GetCategoryAsync(categoryId);
            if (category == null) return NotFound();

            ViewBag.Category = category;

            // OPCIONAL: preencher dropdown de condomínios
            if (_condos != null)
            {
                var list = (await _condos.GetAllAsync())
                    .OrderBy(c => c.Name)
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                    .ToList();

                if (list.Count > 0)
                    ViewBag.Condominiums = new SelectList(list, "Value", "Text");
            }

            return View();
        }

        // ========= NEW TOPIC (POST) =========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewTopic(int categoryId, string title, string body, int? condominiumId, List<IFormFile>? files)
        {
            var userId = UserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var category = await _repo.GetCategoryAsync(categoryId);
            if (category == null) return NotFound();
            if (category.IsLocked && !(User.IsInRole("Administrator") || User.IsInRole("Manager")))
            {
                TempData["Error"] = "This category is locked.";
                return RedirectToAction(nameof(Category), new { id = categoryId });
            }

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
            {
                TempData["Error"] = "Title and message are required.";
                return RedirectToAction(nameof(NewTopic), new { categoryId });
            }

            // monta o tópico e o primeiro post
            var topic = new ForumTopic
            {
                CategoryId = categoryId,
                Title = title.Trim(),
                CreatedById = userId!,
                CompanyId = null,                
                CondominiumId = condominiumId,   // <- do dropdown (se exibido)
                LastPostAtUtc = DateTime.UtcNow
            };

            var firstPost = new ForumPost
            {
                Body = body.Trim(),
                CreatedById = userId!
            };

            // cria tópico + 1º post (transação dentro do repo)
            var topicId = await _repo.CreateTopicAsync(topic, firstPost);

            // salva anexos do primeiro post (opcional)
            if (files != null && files.Count > 0)
            {
                var saved = new List<ForumAttachment>();
                var folder = Path.Combine(_env.WebRootPath, "uploads", "forum");
                Directory.CreateDirectory(folder);

                foreach (var f in files.Where(f => f?.Length > 0))
                {
                    var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
                    var ok = new[] { ".png", ".jpg", ".jpeg", ".gif", ".pdf", ".doc", ".docx" }.Contains(ext);
                    if (!ok) continue;
                    if (f.Length > 8 * 1024 * 1024) continue; // 8MB

                    var safeBase = string.Join("_", Path.GetFileNameWithoutExtension(f.FileName)
                        .Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
                    var name = $"{Guid.NewGuid():N}_{safeBase}{ext}";
                    var full = Path.Combine(folder, name);

                    using (var fs = System.IO.File.Create(full))
                        await f.CopyToAsync(fs);

                    saved.Add(new ForumAttachment
                    {
                        PostId = firstPost.Id,                         // já tem Id após CreateTopicAsync
                        FileName = Path.GetFileName(f.FileName),
                        Path = $"/uploads/forum/{name}",
                        ContentType = f.ContentType ?? "application/octet-stream",
                        Size = f.Length
                    });
                }

                if (saved.Count > 0)
                    await _repo.AddAttachmentsAsync(saved);            // método no repositório
            }

            // assina o autor por padrão (opcional)
            await _repo.SubscribeAsync(topicId, userId!);

            TempData["Success"] = "Topic created.";
            return RedirectToAction(nameof(Topic), new { id = topicId });
        }
        // POST: /Forum/Reply
   

        // POST: /Forum/Subscribe
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Subscribe(int topicId)
        {
            await _repo.SubscribeAsync(topicId, UserId()!);
            return RedirectToAction(nameof(Topic), new { id = topicId });
        }

        // POST: /Forum/Unsubscribe
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Unsubscribe(int topicId)
        {
            await _repo.UnsubscribeAsync(topicId, UserId()!);
            return RedirectToAction(nameof(Topic), new { id = topicId });
        }

        // Moderation (staff)
        [Authorize(Roles = "Administrator,Manager")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Pin(int topicId, bool pin = true)
        {
            await _repo.TogglePinAsync(topicId, pin);
            return RedirectToAction(nameof(Topic), new { id = topicId });
        }

        [Authorize(Roles = "Administrator,Manager")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Lock(int topicId, bool locked = true)
        {
            await _repo.ToggleLockAsync(topicId, locked);
            return RedirectToAction(nameof(Topic), new { id = topicId });
        }

        // Soft-delete own post (or staff can delete any)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int id, int topicId)
        {
            if (User.IsInRole("Administrator") || User.IsInRole("Manager"))
            {
                await _repo.SoftDeletePostAsync(id, UserId()!);
                return RedirectToAction(nameof(Topic), new { id = topicId });
            }

            // basic check: only owner can delete own post
            var posts = await _repo.GetPostsAsync(topicId, 1, int.MaxValue);
            var post = posts.FirstOrDefault(p => p.Id == id);
            if (post == null) return NotFound();
            if (post.CreatedById != UserId()) return Forbid();

            await _repo.SoftDeletePostAsync(id, UserId()!);
            return RedirectToAction(nameof(Topic), new { id = topicId });
        }


        // ========= Reply (POST) =========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int topicId, string body, List<IFormFile>? files)
        {
            var userId = UserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var topic = await _repo.GetTopicAsync(topicId);
            if (topic == null) return NotFound();

            var isStaff = User.IsInRole("Administrator") || User.IsInRole("Manager");
            if (topic.IsLocked && !isStaff) return Forbid();

            if (string.IsNullOrWhiteSpace(body))
            {
                TempData["Error"] = "Message can't be empty.";
                return RedirectToAction(nameof(Topic), new { id = topicId });
            }

            // cria post
            var post = new ForumPost
            {
                TopicId = topicId,
                Body = body.Trim(),
                CreatedById = userId,
                CreatedAtUtc = DateTime.UtcNow
            };
            await _repo.AddPostAsync(post);

            // anexos (opcional)
            var saved = new List<ForumAttachment>();
            if (files != null && files.Count > 0)
            {
                var folder = Path.Combine(_env.WebRootPath, "uploads", "forum");
                Directory.CreateDirectory(folder);

                var allow = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    { ".png", ".jpg", ".jpeg", ".gif", ".pdf", ".doc", ".docx" };
                const long MAX = 8L * 1024 * 1024;

                foreach (var f in files.Where(f => f != null && f.Length > 0))
                {
                    var ext = Path.GetExtension(f.FileName) ?? "";
                    if (!allow.Contains(ext)) continue;
                    if (f.Length > MAX) continue;

                    var safeBase = string.Join("_",
                        Path.GetFileNameWithoutExtension(f.FileName)
                            .Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
                    var name = $"{Guid.NewGuid():N}_{safeBase}{ext}";
                    var full = Path.Combine(folder, name);

                    using (var fs = System.IO.File.Create(full))
                        await f.CopyToAsync(fs);

                    saved.Add(new ForumAttachment
                    {
                        PostId = post.Id,
                        FileName = Path.GetFileName(f.FileName),
                        Path = $"/uploads/forum/{name}",
                        ContentType = f.ContentType ?? "application/octet-stream",
                        Size = f.Length,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }

                if (saved.Count > 0)
                    await _repo.AddAttachmentsAsync(saved);
            }

            // atualiza last activity
            await _repo.TouchTopicAsync(topicId);

            // URL do tópico
            var topicUrl = Url.Action(nameof(Topic), "Forum", new { id = topicId }, Request.Scheme)!;

            // notificar inscritos (exclui o próprio autor pelo e-mail)
            try
            {
                var recipients = await _repo.GetSubscriberEmailsAsync(topicId);
                var me = UserEmail();
                if (!string.IsNullOrWhiteSpace(me))
                    recipients = recipients
                        .Where(e => !string.Equals(e, me, StringComparison.OrdinalIgnoreCase))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                if (recipients.Count > 0)
                {
                    var preview = post.Body.Length > 200 ? post.Body[..200] + "..." : post.Body;
                    await _notify.ForumNewPostAsync(recipients, topic.Title, preview, topicUrl);
                }
            }
            catch { /* não quebra UX */ }

       
            try
            {
                var emailMentions = ExtractEmailsFromMentions(post.Body);
                var me = UserEmail();
                emailMentions = emailMentions
                    .Where(e => !string.Equals(e, me, StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (emailMentions.Count > 0)
                {
                    var preview = post.Body.Length > 200 ? post.Body[..200] + "..." : post.Body;
                    await _notify.ForumMentionAsync(emailMentions, topic.Title, preview, topicUrl);
                }
            }
            catch { }

            TempData["Success"] = "Message posted.";
            // vai para a última página
            var total = await _repo.CountPostsAsync(topicId);
            var pageSize = 20;
            var lastPage = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            return RedirectToAction(nameof(Topic), new { id = topicId, page = lastPage });
        }

        // ========= Download de anexo =========
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Attachment(int id)
        {
            var att = await _repo.GetAttachmentAsync(id);
            if (att == null) return NotFound();

            var rel = att.Path.TrimStart('/');
            var full = Path.Combine(_env.WebRootPath, rel.Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(full)) return NotFound();

            var contentType = string.IsNullOrWhiteSpace(att.ContentType) ? "application/octet-stream" : att.ContentType;
            return PhysicalFile(full, contentType, att.FileName);
        }

        // ========= Restore (undelete) =========
        [HttpPost]
        [Authorize(Roles = "Administrator,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestorePost(int id, int topicId)
        {
            await _repo.RestorePostAsync(id);
            TempData["Success"] = "Post restored.";
            return RedirectToAction(nameof(Topic), new { id = topicId });
        }

        // helper: extrai e-mails após '@'
        private static List<string> ExtractEmailsFromMentions(string text)
        {
            // pega @email@domínio.tld simples
            var rx = new System.Text.RegularExpressions.Regex(@"@([A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,})",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return rx.Matches(text ?? "")
                     .Select(m => m.Groups[1].Value)
                     .Where(s => !string.IsNullOrWhiteSpace(s))
                     .ToList();
        }



        [Authorize(Roles = "Administrator,Manager")]
        [HttpGet]
        public IActionResult NewCategory() => View();

        [Authorize(Roles = "Administrator,Manager")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> NewCategory(string name, int sortOrder = 1, bool isLocked = false,
                                                    int? companyId = null, int? condominiumId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Name is required.";
                return View();
            }

            var c = new ForumCategory
            {
                Name = name.Trim(),
                SortOrder = sortOrder,
                IsLocked = isLocked,
                CompanyId = companyId,
                CondominiumId = condominiumId
            };
            await _repo.CreateCategoryAsync(c);
            TempData["Success"] = "Category created.";
            return RedirectToAction(nameof(Index));
        }



    }
}


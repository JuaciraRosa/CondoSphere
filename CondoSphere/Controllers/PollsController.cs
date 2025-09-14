using CondoSphere.Data.Interfaces;
using CondoSphere.Messaging;
using CondoSphere.Models;
using CondoSphere.ViewModels.Polls;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CondoSphere.Controllers
{
    [Authorize(Roles = "Resident,Administrator,Manager")]
    public class PollsController : Controller
    {
        private readonly IPollRepository _polls;
        private readonly DomainNotificationService _notify;
       

        public PollsController(IPollRepository polls, DomainNotificationService notify) 
        {
            _polls = polls;
            _notify = notify;
        }

        private string? UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = UserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var list = await _polls.GetOpenForUserAsync(userId);

            var voted = new Dictionary<int, bool>();
            foreach (var p in list)
                voted[p.Id] = await _polls.HasUserVotedAsync(p.Id, userId);

            ViewBag.UserVotes = voted;
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Cast(int id)
        {
            var poll = await _polls.GetDetailsAsync(id);
            if (poll == null) return NotFound();

            var userId = UserId()!;
            ViewBag.AlreadyVoted = await _polls.HasUserVotedAsync(id, userId);
            return View(poll);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Cast(int id, int optionId)
        {
            var userId = UserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                await _polls.UpsertVoteAsync(id, optionId, userId);

                // ===== Enviar e-mail de confirmação do voto =====
                var email = User.FindFirstValue(ClaimTypes.Email);
                if (!string.IsNullOrWhiteSpace(email))
                {
                    var poll = await _polls.GetDetailsAsync(id);                    // título + opções
                    var option = poll?.Options.FirstOrDefault(o => o.Id == optionId); // texto da opção

                    if (poll != null && option != null && !string.IsNullOrWhiteSpace(option.Text))
                    {
                        // método do seu DomainNotificationService
                        await _notify.PollVoteReceiptAsync(email, poll.Title, option.Text);

                    }
                }
                // =================================================

                TempData["Success"] = "Voto registado!";
                return RedirectToAction(nameof(Result), new { id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                var poll = await _polls.GetDetailsAsync(id);
                return View(poll!);
            }
        }


        [HttpGet]
        public async Task<IActionResult> Result(int id)
        {
            var poll = await _polls.GetDetailsAsync(id);
            if (poll == null) return NotFound();

            var total = poll.Votes.Count;

            var items = poll.Options
                .OrderBy(o => o.Text) // sem propriedade Order; ordena por texto
                .Select(o =>
                {
                    var count = poll.Votes.Count(v => v.OptionId == o.Id);
                    var pct = total == 0 ? 0 : (int)Math.Round(100.0 * count / total);
                    return new PollResultItemVM(o.Id, o.Text, count, pct);
                })
                .ToList();

            var vm = new PollResultVM
            {
                Poll = poll,
                Total = total,
                Items = items
            };

            return View(vm);
        }
    }
}

using CondoSphere.Data;
using CondoSphere.Data.Interfaces;
using CondoSphere.Features.Voting;
using CondoSphere.Messaging;
using CondoSphere.Models;
using CondoSphere.Services.AppData;
using CondoSphere.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;



namespace CondoSphere.Controllers
{
    [Authorize(Roles = "Resident,Administrator,Manager")]
    public class VotingController : Controller
    {
        private readonly IVotingRepository _repo;
        private readonly DomainNotificationService _notify;

        public VotingController(IVotingRepository repo, DomainNotificationService notify)
        {
            _repo = repo;
            _notify = notify;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var userMail = User.FindFirstValue(ClaimTypes.Email) ?? "";
            var vm = await _repo.ListMeetingsForUserAsync(userId, userMail);
            return View(vm);
        }

        // alias para manter roteamento existente
        [HttpGet] public Task<IActionResult> Vote(int meetingId) => Cast(meetingId);
        [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Vote(VoteCastVM model) => Cast(model);

        [HttpGet]
        public async Task<IActionResult> Cast(int meetingId)
        {
            var email = User.Identity?.IsAuthenticated == true ? (User.FindFirstValue(ClaimTypes.Email) ?? "") : "";
            var vm = new VoteCastVM
            {
                MeetingId = meetingId,
                Email = email,
                Units = await _repo.GetUnitsForMeetingAsync(meetingId),
                AlreadyVoted = await _repo.HasUserVotedAsync(meetingId, email)
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cast(VoteCastVM model)
        {
            model.Units = await _repo.GetUnitsForMeetingAsync(model.MeetingId);
            if (!ModelState.IsValid) return View(model);

            if (await _repo.HasUserVotedAsync(model.MeetingId, model.Email))
            {
                model.AlreadyVoted = true;
                TempData["ok"] = "Você já participou nesta votação.";
                return View(model);
            }

            await _repo.UpsertVoteAsync(new MeetingVoteDto
            {
                MeetingId = model.MeetingId,
                VoterEmail = model.Email,
                UnitNumber = model.UnitNumber,
                Choice = model.Choice
            });

            await _notify.VotingThankYouAsync(model.Email, model.MeetingId, model.UnitNumber, model.Choice);

            TempData["ok"] = "Voto registado!";
            return RedirectToAction(nameof(Result), new { meetingId = model.MeetingId });
        }

        [HttpGet]
        public async Task<IActionResult> Result(int meetingId)
        {
            var res = await _repo.GetResultAsync(meetingId);
            return View(res);
        }
    }

}


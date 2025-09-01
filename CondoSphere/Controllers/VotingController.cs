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

    [Authorize]
    public class VotingController : Controller
    {
        private readonly JsonFileStore<MeetingVoteDto> _store;
        private readonly DomainNotificationService _notify;
        private readonly ApplicationDbContext _db; // para carregar Unidades

        public VotingController(
            IWebHostEnvironment env,
            DomainNotificationService notify,
            ApplicationDbContext db)
        {
            _store = new JsonFileStore<MeetingVoteDto>(env, "appdata/votes.json");
            _notify = notify;
            _db = db;
        }

        // carrega todas as unidades (se quiser, filtre por condomínio da reunião)
        private async Task<IEnumerable<SelectListItem>> GetUnitsAsync(int meetingId)
        {
            return await _db.Units
                .OrderBy(u => u.Number)
                .Select(u => new SelectListItem { Value = u.Number, Text = u.Number })
                .ToListAsync();
        }

        [HttpGet]
        public async Task<IActionResult> Cast(int meetingId)
        {
            var vm = new VoteCastVM
            {
                MeetingId = meetingId,
                Email = User?.Identity?.IsAuthenticated == true
                    ? (User.FindFirstValue(ClaimTypes.Email) ?? "")
                    : ""
            };

            vm.Units = await GetUnitsAsync(meetingId);

            var list = await _store.ReadAllAsync();
            if (!string.IsNullOrWhiteSpace(vm.Email))
            {
                vm.AlreadyVoted = list.Any(v =>
                    v.MeetingId == meetingId &&
                    v.VoterEmail.Equals(vm.Email, StringComparison.OrdinalIgnoreCase));
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cast(VoteCastVM model)
        {
            model.Units = await GetUnitsAsync(model.MeetingId);
            if (!ModelState.IsValid) return View(model);

            var list = await _store.ReadAllAsync();

            var already = list.Any(v =>
                v.MeetingId == model.MeetingId &&
                v.VoterEmail.Equals(model.Email, StringComparison.OrdinalIgnoreCase));

            model.AlreadyVoted = already;
            if (already)
            {
                TempData["ok"] = "Você já participou nesta votação.";
                return View(model);
            }

            // garante 1 voto por MeetingId+Email
            list.RemoveAll(v =>
                v.MeetingId == model.MeetingId &&
                v.VoterEmail.Equals(model.Email, StringComparison.OrdinalIgnoreCase));

            list.Add(new MeetingVoteDto
            {
                Id = Guid.NewGuid().ToString(),
                MeetingId = model.MeetingId,
                VoterEmail = model.Email,
                UnitNumber = model.UnitNumber,
                Choice = model.Choice,
                CreatedAt = DateTime.UtcNow
            });
            await _store.WriteAllAsync(list);

            // e-mail de agradecimento
            await _notify.VotingThankYouAsync(model.Email, model.MeetingId, model.UnitNumber, model.Choice);

            TempData["ok"] = "Voto registado!";
            return RedirectToAction(nameof(Result), new { meetingId = model.MeetingId });
        }

        [HttpGet]
        public async Task<IActionResult> Result(int meetingId)
        {
            var list = await _store.ReadAllAsync();
            var m = list.Where(x => x.MeetingId == meetingId).ToList();
            var res = new MeetingVoteResultDto
            {
                MeetingId = meetingId,
                Total = m.Count,
                AFavor = m.Count(x => x.Choice == "A favor"),
                Contra = m.Count(x => x.Choice == "Contra"),
                Abstencao = m.Count(x => x.Choice == "Abstenção")
            };
            return View(res);
        }
    }
}


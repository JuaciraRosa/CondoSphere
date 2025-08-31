using CondoSphere.Features.Voting;
using CondoSphere.Services.AppData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CondoSphere.Controllers
{

    [Authorize]
    public class VotingController : Controller
    {
        private readonly JsonFileStore<MeetingVoteDto> _store;

        public VotingController(IWebHostEnvironment env)
        {
            _store = new JsonFileStore<MeetingVoteDto>(env, "appdata/votes.json");
        }

        // GET: /Voting/Cast?meetingId=5
        [HttpGet]
        public IActionResult Cast(int meetingId)
        {
            ViewBag.MeetingId = meetingId;
            return View();
        }

        // POST: /Voting/Cast
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cast(MeetingVoteDto model)
        {
            if (!ModelState.IsValid) return View(model);
            var list = await _store.ReadAllAsync();

            // 1 voto por (MeetingId + VoterEmail) — substitui voto anterior
            list.RemoveAll(v => v.MeetingId == model.MeetingId && v.VoterEmail.Equals(model.VoterEmail, StringComparison.OrdinalIgnoreCase));
            model.CreatedAt = DateTime.UtcNow;
            list.Add(model);
            await _store.WriteAllAsync(list);

            TempData["ok"] = "Voto registado!";
            return RedirectToAction(nameof(Result), new { meetingId = model.MeetingId });
        }

        // GET: /Voting/Result?meetingId=5
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

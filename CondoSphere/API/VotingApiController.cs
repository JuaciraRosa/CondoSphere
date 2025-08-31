using CondoSphere.Services.AppData;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CondoSphere.API
{
    [Route("api/voting")]
    [ApiController]
    [Authorize]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class VotingApiController : ControllerBase
    {
        private readonly JsonFileStore<CondoSphere.Features.Voting.MeetingVoteDto> _store;

        public VotingApiController(IWebHostEnvironment env)
        {
            _store = new JsonFileStore<CondoSphere.Features.Voting.MeetingVoteDto>(env, "appdata/votes.json");
        }

        [HttpPost("cast")]
        public async Task<IActionResult> Cast([FromBody] CondoSphere.Features.Voting.MeetingVoteDto model)
        {
            var list = await _store.ReadAllAsync();
            list.RemoveAll(v => v.MeetingId == model.MeetingId && v.VoterEmail.Equals(model.VoterEmail, StringComparison.OrdinalIgnoreCase));
            model.CreatedAt = DateTime.UtcNow;
            list.Add(model);
            await _store.WriteAllAsync(list);
            return Ok(new { ok = true });
        }

        [HttpGet("result/{meetingId:int}")]
        public async Task<IActionResult> Result(int meetingId)
        {
            var list = await _store.ReadAllAsync();
            var m = list.Where(x => x.MeetingId == meetingId).ToList();
            var res = new CondoSphere.Features.Voting.MeetingVoteResultDto
            {
                MeetingId = meetingId,
                Total = m.Count,
                AFavor = m.Count(x => x.Choice == "A favor"),
                Contra = m.Count(x => x.Choice == "Contra"),
                Abstencao = m.Count(x => x.Choice == "Abstenção")
            };
            return Ok(res);
        }
    }
}

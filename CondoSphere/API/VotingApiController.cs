using CondoSphere.Features.Voting;
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
        public async Task<IActionResult> Cast([FromBody] MeetingVoteDto model)
        {
            var list = await _store.ReadAllAsync();

            list.RemoveAll(v => v.MeetingId == model.MeetingId &&
                                v.VoterEmail.Equals(model.VoterEmail, StringComparison.OrdinalIgnoreCase));

            var vote = new MeetingVoteDto
            {
                MeetingId = model.MeetingId,
                VoterEmail = model.VoterEmail?.Trim(),
                UnitNumber = model.UnitNumber?.Trim(),
                Choice = model.Choice,
                CreatedAt = DateTime.UtcNow   // ✅ permitido no inicializador
            };

            list.Add(vote);
            await _store.WriteAllAsync(list);
            return Ok(vote);
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

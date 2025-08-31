using CondoSphere.Services.AppData;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CondoSphere.API
{
    [Route("api/occurrences")]
    [ApiController]
    [Authorize]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class OccurrencesApiController : ControllerBase
    {
        private readonly JsonFileStore<CondoSphere.Features.Ocurrences.OccurrenceDto> _store;

        public OccurrencesApiController(IWebHostEnvironment env)
        {
            _store = new JsonFileStore<CondoSphere.Features.Ocurrences.OccurrenceDto>(env, "appdata/occurrences.json");
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int? condominiumId)
        {
            var list = await _store.ReadAllAsync();
            if (condominiumId.HasValue) list = list.Where(x => x.CondominiumId == condominiumId.Value).ToList();
            return Ok(list.OrderByDescending(x => x.CreatedAt));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CondoSphere.Features.Ocurrences.OccurrenceDto model)
        {
            var list = await _store.ReadAllAsync();
            model.CreatedAt = DateTime.UtcNow;
            list.Add(model);
            await _store.WriteAllAsync(list);
            return Ok(model);
        }

        [HttpPost("{id}/status")]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> ChangeStatus(string id, [FromQuery] string status)
        {
            var list = await _store.ReadAllAsync();
            var oc = list.FirstOrDefault(x => x.Id == id);
            if (oc == null) return NotFound();
            oc.Status = status;
            if (status == "Resolved") oc.ClosedAt = DateTime.UtcNow;
            await _store.WriteAllAsync(list);
            return Ok(oc);
        }
    }
}

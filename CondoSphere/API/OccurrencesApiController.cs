using CondoSphere.Features.Ocurrences;
using CondoSphere.Services.AppData;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CondoSphere.API
{
    [Route("api/occurrences")]
    [ApiController]
    // exige JWT para todas as ações
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class OccurrencesApiController : ControllerBase
    {
        private readonly JsonFileStore<Occurrence> _store;

        public OccurrencesApiController(IWebHostEnvironment env)
        {
            _store = new JsonFileStore<Occurrence>(env, "appdata/occurrences.json");
        }

        // Resident + Admin + Manager podem LISTAR
        [HttpGet]
        [Authorize(Roles = "Administrator,Manager,Resident")]
        public async Task<IActionResult> List([FromQuery] int? condominiumId)
        {
            var list = await _store.ReadAllAsync();
            if (condominiumId.HasValue)
                list = list.Where(x => x.CondominiumId == condominiumId.Value).ToList();

            return Ok(list.OrderByDescending(x => x.CreatedAt));
        }

        // Só Admin/Manager podem CRIAR
        [HttpPost]
        [Authorize(Roles = "Administrator,Manager,Resident")]
        public async Task<IActionResult> Create([FromBody] Occurrence model)
        {
            if (model is null) return BadRequest(new { error = "Payload inválido." });

            // garante CreatedBy (usa claim de e-mail se não vier no body)
            if (string.IsNullOrWhiteSpace(model.CreatedBy))
            {
                var emailHeader = Request.Headers["X-User-Email"].ToString();
                var emailClaim = User?.FindFirst(ClaimTypes.Email)?.Value;
                model.CreatedBy = (emailHeader ?? emailClaim ?? "anonymous@local")
                                  .Trim().ToLowerInvariant();
            }

            model.CreatedAt = DateTime.UtcNow;

            var list = await _store.ReadAllAsync();
            list.Add(model);
            await _store.WriteAllAsync(list);

            // podes devolver 201 Created
            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        // GET por id — todos os roles autenticados
        [HttpGet("{id}")]
        [Authorize(Roles = "Administrator,Manager,Resident")]
        public async Task<IActionResult> GetById(string id)
        {
            var list = await _store.ReadAllAsync();
            var oc = list.FirstOrDefault(x => x.Id == id);
            if (oc == null) return NotFound();
            return Ok(oc);
        }

        // Alterar status — apenas Admin/Manager
        [HttpPost("{id}/status")]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> ChangeStatus(string id, [FromQuery] string status)
        {
            var list = await _store.ReadAllAsync();
            var oc = list.FirstOrDefault(x => x.Id == id);
            if (oc == null) return NotFound();

            oc.Status = status;
            if (string.Equals(status, "Resolved", StringComparison.OrdinalIgnoreCase))
                oc.ClosedAt = DateTime.UtcNow;

            await _store.WriteAllAsync(list);
            return Ok(oc);
        }
    }
}

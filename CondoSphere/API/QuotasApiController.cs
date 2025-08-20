using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CondoSphere.API
{
    [ApiController]
    [Route("api/quotas")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class QuotasApiController : ControllerBase
    {
        private readonly IQuotaRepository _repository;

        public QuotasApiController(IQuotaRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetAll() => Ok(await _repository.GetAllAsync());

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var quota = await _repository.GetByIdAsync(id);
            if (quota == null)
                return NotFound(new ProblemDetails { Title = "Not found", Detail = $"Quota {id} not found", Status = 404 });

            return Ok(quota);
        }

        [HttpGet("by-unit/{unitId:int}")]
        public async Task<IActionResult> GetByUnit(int unitId)
        {
            var data = (await _repository.GetAllAsync()).Where(q => q.UnitId == unitId);
            return Ok(data);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Create([FromBody] Quota quota)
        {
            if (!ModelState.IsValid) return BadRequest(new ValidationProblemDetails(ModelState));
            await _repository.AddAsync(quota);
            return CreatedAtAction(nameof(Get), new { id = quota.Id }, quota);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Update(int id, [FromBody] Quota quota)
        {
            if (id != quota.Id) return BadRequest(new ProblemDetails { Title = "ID mismatch", Status = 400 });
            _repository.Update(quota);
            await _repository.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            await _repository.DeleteAsync(id);
            return NoContent();
        }
    }
}

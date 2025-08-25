using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CondoSphere.API
{
    [Route("api/units")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UnitsApiController : ControllerBase
    {
        private readonly IUnitRepository _repository;

        public UnitsApiController(IUnitRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("list")]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> GetAll() => Ok(await _repository.GetAllAsync());

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var unit = await _repository.GetByIdAsync(id);
            if (unit == null)
                return NotFound(new ProblemDetails { Title = "Not found", Detail = $"Unit {id} not found", Status = 404 });

            return Ok(unit);
        }

        // Unidades do residente logado
        [HttpGet("mine")]
        [Authorize(Roles = "Resident")]
        public async Task<IActionResult> Mine()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var data = (await _repository.GetAllAsync()).Where(u => u.OwnerId == userId);
            return Ok(data);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Create([FromBody] Unit unit)
        {
            if (!ModelState.IsValid) return BadRequest(new ValidationProblemDetails(ModelState));
            await _repository.AddAsync(unit);
            return CreatedAtAction(nameof(Get), new { id = unit.Id }, unit);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Update(int id, [FromBody] Unit unit)
        {
            if (id != unit.Id) return BadRequest(new ProblemDetails { Title = "ID mismatch", Status = 400 });
            _repository.Update(unit);
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


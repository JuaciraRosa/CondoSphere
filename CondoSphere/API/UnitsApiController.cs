using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CondoSphere.API
{
    [Route("api/[controller]")]
    [ApiController]

    public class UnitsApiController : ControllerBase
    {
        private readonly IUnitRepository _repository;

        public UnitsApiController(IUnitRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _repository.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var unit = await _repository.GetByIdAsync(id);
            if (unit == null) return NotFound();
            return Ok(unit);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Unit unit)
        {
            await _repository.AddAsync(unit);
            return CreatedAtAction(nameof(Get), new { id = unit.Id }, unit);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, Unit unit)
        {
            if (id != unit.Id) return BadRequest();
            _repository.Update(unit);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _repository.DeleteAsync(id);
            return NoContent();
        }
    }
}

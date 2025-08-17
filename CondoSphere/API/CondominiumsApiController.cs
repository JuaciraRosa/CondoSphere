using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CondoSphere.API
{
    [Route("api/[controller]")]
    [ApiController]

    public class CondominiumsApiController : ControllerBase
    {
        private readonly ICondominiumRepository _repository;

        public CondominiumsApiController(ICondominiumRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _repository.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Condominium condo)
        {
            await _repository.AddAsync(condo);
            return CreatedAtAction(nameof(Get), new { id = condo.Id }, condo);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, Condominium condo)
        {
            if (id != condo.Id) return BadRequest();
            _repository.Update(condo);
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


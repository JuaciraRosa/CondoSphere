using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CondoSphere.API
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuotasApiController : ControllerBase
    {
        private readonly IQuotaRepository _repository;

        public QuotasApiController(IQuotaRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _repository.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var quota = await _repository.GetByIdAsync(id);
            if (quota == null) return NotFound();
            return Ok(quota);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Quota quota)
        {
            await _repository.AddAsync(quota);
            return CreatedAtAction(nameof(Get), new { id = quota.Id }, quota);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, Quota quota)
        {
            if (id != quota.Id) return BadRequest();
            _repository.Update(quota);
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

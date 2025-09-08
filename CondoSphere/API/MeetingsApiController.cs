using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CondoSphere.API
{
    [Route("api/meetings")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class MeetingsApiController : ControllerBase
    {
        private readonly IMeetingRepository _repository;

        public MeetingsApiController(IMeetingRepository repository)
        {
            _repository = repository;
        }

        // Todos autenticados podem listar (resident precisa ver as reuniões)
        [HttpGet("list")]
        public async Task<IActionResult> GetAll() => Ok(await _repository.GetAllAsync());

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var meeting = await _repository.GetByIdAsync(id);
            if (meeting == null)
                return NotFound(new ProblemDetails { Title = "Not found", Detail = $"Meeting {id} not found", Status = 404 });

            return Ok(meeting);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Create([FromBody] Meeting meeting)
        {
            if (!ModelState.IsValid) return BadRequest(new ValidationProblemDetails(ModelState));
            await _repository.AddAsync(meeting);
            return CreatedAtAction(nameof(Get), new { id = meeting.Id }, meeting);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Update(int id, [FromBody] Meeting meeting)
        {
            if (id != meeting.Id) return BadRequest(new ProblemDetails { Title = "ID mismatch", Status = 400 });
            _repository.Update(meeting);
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

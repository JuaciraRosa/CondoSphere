using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CondoSphere.API
{
    [Route("api/meetings")]
    [ApiController]
    public class MeetingsApiController : ControllerBase
    {
        private readonly IMeetingRepository _repository;

        public MeetingsApiController(IMeetingRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Meeting>>> GetAll()
        {
            var meetings = await _repository.GetAllAsync();
            return Ok(meetings);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Meeting>> Get(int id)
        {
            var meeting = await _repository.GetByIdAsync(id);
            if (meeting == null) return NotFound();
            return Ok(meeting);
        }

        [HttpPost]
        public async Task<ActionResult> Create(Meeting meeting)
        {
            await _repository.AddAsync(meeting);
            return CreatedAtAction(nameof(Get), new { id = meeting.Id }, meeting);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, Meeting meeting)
        {
            if (id != meeting.Id) return BadRequest();
            _repository.Update(meeting);
            await _repository.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            await _repository.DeleteAsync(id);
            return NoContent();
        }
    }
}

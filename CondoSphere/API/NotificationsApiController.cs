using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CondoSphere.API
{
    [Route("api/notifications")]
    [ApiController]
    public class NotificationsApiController : ControllerBase
    {
        private readonly INotificationRepository _repository;

        public NotificationsApiController(INotificationRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Notification>>> GetAll()
        {
            var notifications = await _repository.GetAllAsync();
            return Ok(notifications);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Notification>> Get(int id)
        {
            var notification = await _repository.GetByIdAsync(id);
            if (notification == null) return NotFound();
            return Ok(notification);
        }

        [HttpPost]
        public async Task<ActionResult> Create(Notification notification)
        {
            await _repository.AddAsync(notification);
            return CreatedAtAction(nameof(Get), new { id = notification.Id }, notification);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, Notification notification)
        {
            if (id != notification.Id) return BadRequest();
            _repository.Update(notification);
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

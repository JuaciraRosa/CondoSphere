using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CondoSphere.API
{
    [Route("api/notifications")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class NotificationsApiController : ControllerBase
    {
        private readonly INotificationRepository _repository;

        public NotificationsApiController(INotificationRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetAll() => Ok(await _repository.GetAllAsync());

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var n = await _repository.GetByIdAsync(id);
            if (n == null)
                return NotFound(new ProblemDetails { Title = "Not found", Detail = $"Notification {id} not found", Status = 404 });

            return Ok(n);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Create([FromBody] Notification notification)
        {
            if (!ModelState.IsValid) return BadRequest(new ValidationProblemDetails(ModelState));
            await _repository.AddAsync(notification);
            return CreatedAtAction(nameof(Get), new { id = notification.Id }, notification);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Update(int id, [FromBody] Notification notification)
        {
            if (id != notification.Id) return BadRequest(new ProblemDetails { Title = "ID mismatch", Status = 400 });
            _repository.Update(notification);
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

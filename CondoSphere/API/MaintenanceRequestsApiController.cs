using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CondoSphere.API
{
    [Route("api/maintenance-requests")]
    [ApiController]
    public class MaintenanceRequestsApiController : ControllerBase
    {
        private readonly IMaintenanceRequestRepository _repository;

        public MaintenanceRequestsApiController(IMaintenanceRequestRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MaintenanceRequest>>> GetAll()
        {
            var requests = await _repository.GetAllAsync();
            return Ok(requests);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MaintenanceRequest>> Get(int id)
        {
            var request = await _repository.GetByIdAsync(id);
            if (request == null) return NotFound();
            return Ok(request);
        }

        [HttpPost]
        public async Task<ActionResult> Create(MaintenanceRequest request)
        {
            await _repository.AddAsync(request);
            return CreatedAtAction(nameof(Get), new { id = request.Id }, request);
        }

        /// <summary>
        /// Create a maintenance request for the logged resident.
        /// </summary>
        [HttpPost("create-my")]
        [Authorize(Roles = "Resident")]
        public async Task<IActionResult> CreateMy([FromBody] MaintenanceRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation error",
                    Detail = "Please check the submitted fields.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = "User not authenticated.",
                    Status = StatusCodes.Status401Unauthorized
                });
            }

            // Force server-side values
            request.SubmittedById = userId;
            request.SubmittedAt = DateTime.UtcNow;
            request.Status = RequestStatus.InProgress;

            await _repository.AddAsync(request);
            return CreatedAtAction(nameof(Get), new { id = request.Id }, request);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, MaintenanceRequest request)
        {
            if (id != request.Id) return BadRequest();
            _repository.Update(request);
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

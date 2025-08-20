using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CondoSphere.API
{
    [Route("api/maintenance-requests")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class MaintenanceRequestsApiController : ControllerBase
    {
        private readonly IMaintenanceRequestRepository _repository;

        public MaintenanceRequestsApiController(IMaintenanceRequestRepository repository)
        {
            _repository = repository;
        }

        // Admin/Manager veem todos
        [HttpGet("list")]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> GetAll() => Ok(await _repository.GetAllAsync());

        // Resident vê só os dele
        [HttpGet("mine")]
        [Authorize(Roles = "Resident")]
        public async Task<IActionResult> Mine()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var data = (await _repository.GetAllAsync())
                       .Where(r => r.SubmittedById == userId);
            return Ok(data);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var request = await _repository.GetByIdAsync(id);
            if (request == null)
                return NotFound(new ProblemDetails { Title = "Not found", Detail = $"Request {id} not found", Status = 404 });

            // Resident só pode ver se for dele
            if (User.IsInRole("Resident") && request.SubmittedById != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                return Forbid();

            return Ok(request);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Create([FromBody] MaintenanceRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(new ValidationProblemDetails(ModelState));
            await _repository.AddAsync(request);
            return CreatedAtAction(nameof(Get), new { id = request.Id }, request);
        }

        /// Resident cria sem enviar SubmittedById (servidor popula)
        [HttpPost("create-my")]
        [Authorize(Roles = "Resident")]
        public async Task<IActionResult> CreateMy([FromBody] MaintenanceRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ValidationProblemDetails(ModelState));

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new ProblemDetails { Title = "Unauthorized", Status = 401 });

            request.SubmittedById = userId;
            request.SubmittedAt = DateTime.UtcNow;
            request.Status = RequestStatus.InProgress;

            await _repository.AddAsync(request);
            return CreatedAtAction(nameof(Get), new { id = request.Id }, request);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Update(int id, [FromBody] MaintenanceRequest request)
        {
            if (id != request.Id) return BadRequest(new ProblemDetails { Title = "ID mismatch", Status = 400 });
            _repository.Update(request);
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

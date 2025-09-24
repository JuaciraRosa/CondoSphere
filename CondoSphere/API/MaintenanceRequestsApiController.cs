using CondoSphere.API.Models;
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
            => _repository = repository;

        [HttpGet("list")]                  // qualquer autenticado
        public async Task<IActionResult> GetAll()
            => Ok(await _repository.GetAllAsync());

        [HttpPost]                         // qualquer autenticado
        public async Task<IActionResult> Create([FromBody] MaintenanceRequest request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            request.SubmittedById ??= User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            request.SubmittedAt = request.SubmittedAt == default ? DateTime.UtcNow : request.SubmittedAt;
            if (request.Status == 0) request.Status = RequestStatus.InProgress;

            await _repository.AddAsync(request);
            return CreatedAtAction(nameof(Create), new { id = request.Id }, request);
        }
    }



}

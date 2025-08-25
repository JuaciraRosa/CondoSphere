using CondoSphere.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CondoSphere.API
{
    [Route("api/residents")]
    [ApiController]
    [Authorize(Roles = "Resident")]
    public class ResidentsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ResidentsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/residents/me
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
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

            // Carregar residente com Units -> Quotas -> Payment
            var resident = await _context.Users
                .Include(u => u.OwnedUnits)
                    .ThenInclude(unit => unit.Condominium)
                .Include(u => u.OwnedUnits)
                    .ThenInclude(u => u.Quotas)
                        .ThenInclude(q => q.Payment)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (resident == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Resident not found",
                    Detail = "The logged in user was not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Pedidos de manutenção submetidos pelo user
            var myRequests = await _context.MaintenanceRequests
                .Where(m => m.SubmittedById == userId)
                .Select(m => new
                {
                    m.Id,
                    m.Title,
                    m.Description,
                    m.Status,
                    m.SubmittedAt,
                    m.CondominiumId
                })
                .ToListAsync();

            var payload = new
            {
                resident.FullName,
                resident.Email,
                Units = resident.OwnedUnits.Select(u => new
                {
                    u.Id,
                    u.Number,
                    Condominium = new { u.CondominiumId, u.Condominium.Name },
                    Quotas = u.Quotas.Select(q => new
                    {
                        q.Id,
                        q.Amount,
                        q.DueDate,
                        q.IsPaid,
                        Payment = q.Payment == null ? null : new
                        {
                            q.Payment.Id,
                            q.Payment.Amount,
                            q.Payment.Method,
                            q.Payment.Status,
                            q.Payment.Provider,
                            q.Payment.ProviderPaymentId,
                            q.Payment.ProviderReference,
                            q.Payment.ReceiptUrl,
                            q.Payment.CreatedAt,
                            q.Payment.PaidAt
                        }
                    })
                }),
                MaintenanceRequests = myRequests
            };

            return Ok(payload);
        }
    }

}


using CondoSphere.Data;
using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.API
{
    [ApiController]
    [Route("api/quotas")]
    [AllowAnonymous] 
    public class QuotasApiController : ControllerBase
    {
        private readonly IQuotaRepository _repository;
        public QuotasApiController(IQuotaRepository repository) => _repository = repository;

        //[HttpGet("list")]
        //public async Task<IActionResult> GetAll() => Ok(await _repository.GetAllAsync());

        //[HttpGet("list")]
        //public async Task<IActionResult> GetAll([FromServices] ApplicationDbContext db)
        //{
        //    // Continua usando o seu repo como está (sem Include)
        //    var quotas = await _repository.GetAllAsync();

        //    // Lê os status dos pagamentos, sem mexer no repo
        //    var payByQuota = await db.Payments
        //        .Select(p => new { p.QuotaId, p.Status })
        //        .ToListAsync();

        //    var map = payByQuota
        //        .GroupBy(x => x.QuotaId)
        //        .ToDictionary(g => g.Key, g => g
        //            .OrderByDescending(_ => 1) // qualquer prioridade, só pra pegar “um”
        //            .First().Status);

        //    var data = quotas.Select(q => new
        //    {
        //        q.Id,
        //        q.UnitId,
        //        q.Amount,
        //        q.DueDate,
        //        q.IsPaid,

        //        // Regra: Paid > Pending > Open
        //        paymentStatus =
        //            q.IsPaid
        //            ? "Paid"
        //            : (map.TryGetValue(q.Id, out var st)
        //                ? (st == PaymentStatusType.Succeeded ? "Paid"
        //                   : (st == PaymentStatusType.Canceled || st == PaymentStatusType.Failed ? "Open" : "Pending"))
        //                : "Open")
        //    });

        //    return Ok(data);
        //}

        [HttpGet("list")]
        public async Task<IActionResult> GetAll([FromServices] ApplicationDbContext db)
        {
            var data = await db.Quotas
                .AsNoTracking()
                .Include(q => q.Payment)
                .Select(q => new
                {
                    // QuotaDto
                    Id = q.Id,
                    UnitId = q.UnitId,
                    Amount = q.Amount,
                    DueDate = q.DueDate,
                    IsPaid = q.IsPaid,
                    DebtorUserId = q.DebtorUserId,
                    DebtorEmail = q.DebtorEmail,
                    DebtorName = q.DebtorName,

                    // PaymentDto embutido (quando existir)
                    Payment = q.Payment == null ? null : new
                    {
                        Id = q.Payment.Id,
                        QuotaId = q.Payment.QuotaId,
                        Amount = q.Payment.Amount,
                        // IMPORTANTE: envie enums como string p/ casar com seu JsonStringEnumConverter no app
                        Method = q.Payment.Method.ToString(),
                        Status = q.Payment.Status.ToString(),
                        Provider = q.Payment.Provider,
                        ProviderPaymentId = q.Payment.ProviderPaymentId,
                        ProviderReference = q.Payment.ProviderReference,
                        ReceiptUrl = q.Payment.ReceiptUrl,
                        CreatedAt = q.Payment.CreatedAt,
                        PaidAt = q.Payment.PaidAt
                    }
                })
                .ToListAsync();

            return Ok(data);
        }


        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var quota = await _repository.GetByIdAsync(id);
            if (quota == null)
                return NotFound(new ProblemDetails { Title = "Not found", Detail = $"Quota {id} not found", Status = 404 });
            return Ok(quota);
        }

        [HttpGet("by-unit/{unitId:int}")]
        public async Task<IActionResult> GetByUnit(int unitId)
            => Ok((await _repository.GetAllAsync()).Where(q => q.UnitId == unitId));

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Quota quota)
        {
            if (!ModelState.IsValid) return BadRequest(new ValidationProblemDetails(ModelState));
            await _repository.AddAsync(quota);
            return CreatedAtAction(nameof(Get), new { id = quota.Id }, quota);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Quota quota)
        {
            if (id != quota.Id) return BadRequest(new ProblemDetails { Title = "ID mismatch", Status = 400 });
            _repository.Update(quota);
            await _repository.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _repository.DeleteAsync(id);
            return NoContent();
        }
    }

}

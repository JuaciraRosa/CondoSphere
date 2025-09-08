using CondoSphere.Data.Interfaces;
using CondoSphere.Dtos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace CondoSphere.API
{
    [ApiController]
    [Route("api/reports")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ReportsApiController : ControllerBase
    {
        private readonly IExpenseRepository _expenses;
        private readonly IPaymentRepository _payments;
        private readonly IQuotaRepository _quotas;

        public ReportsApiController(IExpenseRepository expenses, IPaymentRepository payments, IQuotaRepository quotas)
        {
            _expenses = expenses;
            _payments = payments;
            _quotas = quotas;
        }

        [HttpGet("expenses-by-category")]
        public async Task<IActionResult> ExpensesByCategory([FromQuery] int? year)
        {
            var y = year ?? DateTime.UtcNow.Year;

            var data = (await _expenses.GetAllDetailedAsync())
                .Where(e => e.Date.Year == y)
                .GroupBy(e => string.IsNullOrWhiteSpace(e.Description) ? "Sem Categoria" : e.Description)
                .Select(g => new ExpenseCategorySummaryDto
                {
                    Category = g.Key,
                    Total = g.Sum(x => x.Amount),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            return Ok(data);
        }

        // GET api/reports/payments-summary?year=2025
        [HttpGet("payments-summary")]
        public async Task<IActionResult> PaymentsSummary([FromQuery] int? year)
        {
            var y = year ?? DateTime.UtcNow.Year;

            var data = (await _payments.GetAllDetailedAsync())
                .Where(p => p.PaidAt.HasValue && p.PaidAt.Value.Year == y)
                .GroupBy(p => p.PaidAt!.Value.Month)
                .Select(g => new PaymentsSummaryDto
                {
                    Month = g.Key,
                    MonthName = CultureInfo.GetCultureInfo("pt-PT").DateTimeFormat.GetMonthName(g.Key),
                    Total = g.Sum(x => x.Amount),
                    Count = g.Count()
                })
                .OrderBy(x => x.Month)
                .ToList();

            return Ok(data);
        }

        // GET api/reports/quotas-by-month?year=2025
        [HttpGet("quotas-by-month")]
        public async Task<IActionResult> QuotasByMonth([FromQuery] int? year)
        {
            var y = year ?? DateTime.UtcNow.Year;
            var firstOfThisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            var data = (await _quotas.GetAllDetailedAsync())
                .Where(q => q.DueDate.Year == y)
                .GroupBy(q => q.DueDate.Month)
                .Select(g => new QuotasByMonthDto
                {
                    Month = g.Key,
                    MonthName = CultureInfo.GetCultureInfo("pt-PT").DateTimeFormat.GetMonthName(g.Key),
                    Paid = g.Count(x => x.IsPaid),
                    Late = g.Count(x => !x.IsPaid && new DateTime(y, g.Key, 1) < firstOfThisMonth)
                })
                .OrderBy(x => x.Month)
                .ToList();

            return Ok(data);
        }
    }

}


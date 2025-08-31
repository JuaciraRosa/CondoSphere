using CondoSphere.Data.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace CondoSphere.Controllers
{

    [Authorize]
    public class ReportsController : Controller
    {
        private readonly IExpenseRepository _expenses;
        private readonly IPaymentRepository _payments;
        private readonly IQuotaRepository _quotas;

        public ReportsController(IExpenseRepository expenses, IPaymentRepository payments, IQuotaRepository quotas)
        {
            _expenses = expenses;
            _payments = payments;
            _quotas = quotas;
        }

        public IActionResult Index() => View();

        // /Reports/ExpensesByMonth?year=2025
        public async Task<IActionResult> ExpensesByMonth(int? year)
        {
            var y = year ?? DateTime.Now.Year;
            var data = (await _expenses.GetAllDetailedAsync())
                .Where(e => e.Date.Year == y)
                .GroupBy(e => e.Date.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Total = g.Sum(x => x.Amount),
                    Count = g.Count()
                })
                .OrderBy(x => x.Month)
                .ToList();

            ViewBag.Year = y;
            ViewBag.Items = data.Select(d => new
            {
                MonthName = CultureInfo.GetCultureInfo("pt-PT").DateTimeFormat.GetMonthName(d.Month),
                d.Total,
                d.Count
            }).ToList();

            return View(); // cria uma View simples que lê ViewBag.Items
        }
    

        // /Reports/OutstandingQuotas
        public async Task<IActionResult> OutstandingQuotas()
        {
            var data = (await _quotas.GetAllAsync())
                .Where(q => !q.IsPaid)
                .OrderBy(q => q.DueDate)
                .ToList();
            return View(data);
        }

        // /Reports/PaymentsSummary?year=2025
        public async Task<IActionResult> PaymentsSummary(int? year)
        {
            var y = year ?? DateTime.Now.Year;
            var data = (await _payments.GetAllDetailedAsync())
                .Where(p => p.PaidAt.HasValue && p.PaidAt.Value.Year == y)
                .GroupBy(p => p.PaidAt!.Value.Month)
                .Select(g => new PaymentsSummaryDto
                {
                    Month = g.Key,
                    Total = g.Sum(x => x.Amount),
                    Count = g.Count()
                })
                .OrderBy(x => x.Month)
                .ToList();

            ViewBag.Year = y;
            return View(data);
        }
    }

    public class ExpenseCategorySummaryDto
    {
        public string Category { get; set; }
        public decimal Total { get; set; }
        public int Count { get; set; }
    }

    public class PaymentsSummaryDto
    {
        public int Month { get; set; }
        public decimal Total { get; set; }
        public int Count { get; set; }
    }
}

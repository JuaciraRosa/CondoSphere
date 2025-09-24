using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using CondoSphere.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CondoSphere.Controllers
{
    [Authorize(Roles = "Resident,Manager,Administrator")]
    public class MyAccountController : Controller
    {
        private readonly IUnitRepository _units;
        private readonly IQuotaRepository _quotas;

        public MyAccountController(IUnitRepository units, IQuotaRepository quotas)
        {
            _units = units;
            _quotas = quotas;
        }

        private string? CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
        private string? CurrentUserEmail() => User.FindFirstValue(ClaimTypes.Email);

        // /MyAccount
        public async Task<IActionResult> Index()
        {
            var userId = CurrentUserId();
            var email = CurrentUserEmail();

            // Com Owner incluído (GetAllDetailedAsync já faz Include(Owner))
            var all = await _units.GetAllDetailedAsync();

            var myUnits = all
                .Where(u =>
                    (!string.IsNullOrWhiteSpace(u.OwnerId) && u.OwnerId == userId) ||
                    (!string.IsNullOrWhiteSpace(email) &&
                        u.Owner != null &&
                        string.Equals(u.Owner.Email, email, System.StringComparison.OrdinalIgnoreCase)))
                .OrderBy(u => u.Number)
                .ToList();

            if (myUnits.Count == 0)
                return View(myUnits); // mostra página simpática “sem unidades”

            if (myUnits.Count == 1)
                return RedirectToAction(nameof(Statement), new { unitId = myUnits[0].Id });

            return View(myUnits);
        }

        // /MyAccount/Statement?unitId=123
        public async Task<IActionResult> Statement(int unitId)
        {
            // Segurança: garantir que a unidade é do utilizador
            var userId = CurrentUserId();
            var email = CurrentUserEmail();

            var all = await _units.GetAllDetailedAsync();
            var unit = all.FirstOrDefault(u =>
                u.Id == unitId &&
                (
                    (!string.IsNullOrWhiteSpace(u.OwnerId) && u.OwnerId == userId) ||
                    (!string.IsNullOrWhiteSpace(email) &&
                        u.Owner != null &&
                        string.Equals(u.Owner.Email, email, System.StringComparison.OrdinalIgnoreCase))
                ));

            if (unit == null)
                return Forbid();

            // Quotas da unidade (com Unit incluída)
            var allQuotas = await _quotas.GetAllDetailedAsync();
            var quotas = allQuotas
                .Where(q => q.UnitId == unit.Id)
                .OrderBy(q => q.DueDate)
                .ToList();

            var vm = new AccountStatementViewModel
            {
                UnitId = unit.Id,
                UnitLabel = !string.IsNullOrWhiteSpace(unit.Number) ? unit.Number : $"#{unit.Id}",
                OpeningBalance = 0m
            };

            foreach (var q in quotas)
            {
                // Status para mostrar ao residente
                var status =
                    q.IsPaid ? "Paid"
                    : (q.Payment != null && q.Payment.Status == PaymentStatusType.Succeeded ? "Awaiting approval"
                    : (q.Payment != null && q.Payment.Status == PaymentStatusType.Pending ? "Payment pending"
                    : "Pending"));

                // Débito = valor da quota
                var debit = q.Amount;

                // Crédito só quando a quota está “marcada paga” (aprovada)
                var credit = q.IsPaid ? q.Amount : 0m;

                vm.Lines.Add(new AccountStatementLine
                {
                    Date = q.DueDate,
                    Description = $"Quota {q.DueDate:yyyy-MM}",
                    Debit = debit,
                    Credit = credit,
                    Status = status,
                    PaymentId = q.Payment?.Id
                });
            }

            vm.RunningBalance = vm.OpeningBalance + vm.Lines.Sum(l => l.Debit - l.Credit);

            return View(vm);
        }
    }
}

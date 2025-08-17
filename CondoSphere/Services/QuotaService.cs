using CondoSphere.Data.Interfaces;
using CondoSphere.Models;

namespace CondoSphere.Services
{
    public class QuotaService : IQuotaService
    {
        private readonly IUnitRepository _units;
        private readonly IQuotaRepository _quotas;

        public QuotaService(IUnitRepository units, IQuotaRepository quotas)
        {
            _units = units;
            _quotas = quotas;
        }

        public async Task<int> EnsureMonthlyAsync(int condominiumId, int year, int month, decimal amountPerUnit)
        {
            var units = await _units.GetByCondominiumIdAsync(condominiumId);
            var due = new DateTime(year, month, 1);

            var created = 0;
            foreach (var u in units)
            {
                var exists = await _quotas.ExistsAsync(u.Id, year, month);
                if (exists) continue;

                await _quotas.AddAsync(new Quota
                {
                    UnitId = u.Id,
                    Amount = amountPerUnit,
                    DueDate = due,
                    IsPaid = false
                });
                created++;
            }

            if (created > 0)
                await _quotas.SaveChangesAsync();

            return created;
        }

        public Task<int> EnsureQuarterAsync(int condominiumId, int year, int quarter, decimal amountPerUnit)
        {
            var startMonth = quarter switch
            {
                1 => 1,
                2 => 4,
                3 => 7,
                4 => 10,
                _ => throw new ArgumentOutOfRangeException(nameof(quarter), "Quarter must be 1..4")
            };
            return EnsureMonthlyAsync(condominiumId, year, startMonth, amountPerUnit);
        }

        public async Task<int> EnsureRangeMonthlyAsync(int condominiumId, DateTime from, DateTime to, decimal amountPerUnit)
        {
            var total = 0;
            var cur = new DateTime(from.Year, from.Month, 1);
            var end = new DateTime(to.Year, to.Month, 1);

            while (cur <= end)
            {
                total += await EnsureMonthlyAsync(condominiumId, cur.Year, cur.Month, amountPerUnit);
                cur = cur.AddMonths(1);
            }

            return total;
        }
    }
}

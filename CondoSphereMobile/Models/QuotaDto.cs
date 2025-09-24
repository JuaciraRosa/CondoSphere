using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    public class QuotaDto
    {
        public int Id { get; set; }
        public int UnitId { get; set; }
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsPaid { get; set; }

        public string? DebtorUserId { get; set; }
        public string? DebtorEmail { get; set; }
        public string? DebtorName { get; set; }

        // opcionalmente, podes incluir info do Payment se teu endpoint retornar embutido
        public PaymentDto? Payment { get; set; }
    }
}

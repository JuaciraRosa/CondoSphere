using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    // “shape” das quotas que vêm dentro de OwnedUnits (residents/me)
    public class QuotaOwnedDto
    {
        public int Id { get; set; }
        public int UnitId { get; set; }
        public string UnitNumber { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsPaid { get; set; }
        public PaymentOwnedDto? Payment { get; set; }
    }
}

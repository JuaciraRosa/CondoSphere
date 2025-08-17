using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    public class Expense
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }   // API já está avisando precisão, ok
        public DateTime Date { get; set; }
        public int CondominiumId { get; set; }
    }
}

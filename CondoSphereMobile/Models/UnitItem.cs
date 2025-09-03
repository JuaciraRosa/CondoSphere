using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{

    public class UnitItem
    {
        public int Id { get; set; }
        public string Number { get; set; } = "";
        public decimal Area { get; set; }
        public int CondominiumId { get; set; }
        public string OwnerId { get; set; } = "";
    }
}

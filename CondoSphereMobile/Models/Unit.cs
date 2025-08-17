using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    public class Unit
    {
        public int Id { get; set; }
        public string Number { get; set; }
        public double Area { get; set; }
        public int CondominiumId { get; set; }
        public int OwnerId { get; set; }
    }
}

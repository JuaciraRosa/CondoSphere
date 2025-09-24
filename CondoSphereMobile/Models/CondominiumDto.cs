using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    public class CondominiumDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }       // Name (até 100)
        public string? Address { get; set; }    // Address (até 200)
        public int CompanyId { get; set; }
        // Campos de navegação do backend não são necessários aqui
    }
}

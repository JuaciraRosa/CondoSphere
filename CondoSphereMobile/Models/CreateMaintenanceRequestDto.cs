using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    public class CreateMaintenanceRequestDto
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public int CondominiumId { get; set; }
        // NADA de SubmittedById aqui
    }

}

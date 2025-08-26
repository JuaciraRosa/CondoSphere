using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    public class User
    {
        public string Id { get; set; }          // Identity = string
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }        // se vier como enum no API, mapeie para string
        public bool IsActive { get; set; }
    }
}

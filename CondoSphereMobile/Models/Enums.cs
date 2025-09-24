using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    public class Enums
    {
        public enum PaymentMethodType { Card }
        public enum PaymentStatusType { Pending, RequiresAction, Succeeded, Failed, Canceled }
        public enum RequestStatus { Open, InProgress, Resolved } // espelha o backend
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    public enum RequestStatus
    {
        Open = 0,
        InProgress = 1,
        Completed = 2,
        Rejected = 3,
        Cancelled = 3
    }
}
